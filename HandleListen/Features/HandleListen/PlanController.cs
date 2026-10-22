using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/plans")]
public class PlanController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public PlanController(AppDbContext context, AppNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    private async Task<List<string>> GetAccessibleUserIdsAsync(int planId, string ownerId) =>
        [ownerId, .. await _context.PlanGuests.Where(g => g.PlanId == planId).Select(g => g.UserId).ToListAsync()];

    private bool IsAccessible(Plan plan) =>
        plan.UserId == UserId || _context.PlanGuests.Any(g => g.PlanId == plan.Id && g.UserId == UserId);

    private IQueryable<Plan> AccessiblePlansQuery() =>
        _context.Plans.Where(p => p.UserId == UserId || _context.PlanGuests.Any(g => g.PlanId == p.Id && g.UserId == UserId));

    private IQueryable<PlanDto> ToDto(IQueryable<Plan> query) =>
        query.Join(_context.Users, p => p.UserId, u => u.Id,
            (p, u) => new PlanDto(p.Id, p.Name, p.Type, p.IsActive, p.ActivatedDate, p.DayCount, p.Month, p.Year, p.UserId == UserId, u.Email!));

    private static DateOnly? ResolveDate(Plan plan, PlanTodo todo) => plan.Type switch
    {
        PlanType.Weekly when plan.ActivatedDate is { } activated && todo.DayIndex is { } idx => activated.AddDays(idx),
        PlanType.Monthly when plan.Month is { } m && plan.Year is { } y && todo.DayOfMonth is { } dom => new DateOnly(y, m, dom),
        _ => null
    };

    private PlanTodoDto ToTodoDto(Plan plan, PlanTodo todo) =>
        new(todo.Id, todo.Title, todo.Done, todo.DayIndex, todo.DayOfMonth, todo.Time, ResolveDate(plan, todo), todo.SortOrder);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanDto>>> GetAll()
    {
        var ordered = AccessiblePlansQuery().OrderByDescending(p => p.IsActive).ThenBy(p => p.Name);
        return await ToDto(ordered).ToListAsync();
    }

    [HttpGet("upcoming-todos")]
    public async Task<ActionResult<IEnumerable<UpcomingPlanTodoDto>>> GetUpcomingTodos([FromQuery] DateOnly today, [FromQuery] int days = 30)
    {
        var windowEnd = today.AddDays(days);
        var activePlans = await AccessiblePlansQuery()
            .Where(p => p.IsActive && (p.Type == PlanType.Weekly || p.Type == PlanType.Monthly))
            .ToListAsync();
        var planIds = activePlans.Select(p => p.Id).ToList();
        var plansById = activePlans.ToDictionary(p => p.Id);
        var todos = await _context.PlanTodos.Where(t => planIds.Contains(t.PlanId)).ToListAsync();

        return todos
            .Select(t => (Todo: t, Plan: plansById[t.PlanId], Date: ResolveDate(plansById[t.PlanId], t)))
            .Where(x => x.Date is { } d && d >= today && d <= windowEnd)
            .Select(x => new UpcomingPlanTodoDto(x.Plan.Id, x.Plan.Name, x.Todo.Id, x.Todo.Title, x.Todo.Done, x.Date!.Value, x.Todo.Time))
            .OrderBy(x => x.Date).ThenBy(x => x.Time)
            .ToList();
    }

    [HttpGet("standard-todos")]
    public async Task<ActionResult<IEnumerable<StandardPlanTodoDto>>> GetStandardTodos()
    {
        var activePlans = await AccessiblePlansQuery().Where(p => p.IsActive && p.Type == PlanType.Standard).ToListAsync();
        var planIds = activePlans.Select(p => p.Id).ToList();
        var plansById = activePlans.ToDictionary(p => p.Id);
        var todos = await _context.PlanTodos.Where(t => planIds.Contains(t.PlanId)).OrderBy(t => t.SortOrder).ToListAsync();

        return todos.Select(t => new StandardPlanTodoDto(t.PlanId, plansById[t.PlanId].Name, t.Id, t.Title, t.Done)).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<PlanDto>> Create(CreatePlanRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0) return BadRequest("Navn er påkrævet.");

        var plan = new Plan { UserId = UserId, Name = name, Type = request.Type };

        if (request.Type == PlanType.Weekly)
        {
            var dayCount = request.DayCount ?? 7;
            if (dayCount is < 1 or > 31) return BadRequest("Antal dage skal være mellem 1 og 31.");
            plan.DayCount = dayCount;
        }
        else if (request.Type == PlanType.Monthly)
        {
            if (request.Month is not { } month || month is < 1 or > 12) return BadRequest("Vælg en gyldig måned.");
            var currentYear = DateTime.UtcNow.Year;
            if (request.Year is not { } year || year < currentYear - 1 || year > currentYear + 5) return BadRequest("Vælg et gyldigt år.");
            plan.Month = month;
            plan.Year = year;
        }

        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();

        await _notifier.NotifyPlansChanged([UserId]);
        return await ToDto(_context.Plans.Where(p => p.Id == plan.Id)).FirstAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PlanDetailDto>> GetById(int id)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null || !IsAccessible(plan)) return NotFound();

        var planDto = await ToDto(_context.Plans.Where(p => p.Id == id)).FirstAsync();
        var todos = await _context.PlanTodos
            .Where(t => t.PlanId == id)
            .OrderBy(t => t.DayIndex).ThenBy(t => t.DayOfMonth).ThenBy(t => t.SortOrder)
            .ToListAsync();

        return new PlanDetailDto(planDto, todos.Select(t => ToTodoDto(plan, t)).ToList());
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, UpdatePlanRequest request)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null || !IsAccessible(plan)) return NotFound();

        if (request.Name is { } name && name.Trim().Length > 0) plan.Name = name.Trim();

        var structureChangeRequested =
            (request.DayCount is not null && request.DayCount != plan.DayCount) ||
            (request.Month is not null && request.Month != plan.Month) ||
            (request.Year is not null && request.Year != plan.Year);

        if (structureChangeRequested)
        {
            if (plan.IsActive) return BadRequest("Planens struktur kan ikke ændres, når den er aktiveret.");

            if (plan.Type == PlanType.Weekly && request.DayCount is { } dayCount)
            {
                if (dayCount is < 1 or > 31) return BadRequest("Antal dage skal være mellem 1 og 31.");
                plan.DayCount = dayCount;
            }

            if (plan.Type == PlanType.Monthly)
            {
                if (request.Month is { } month)
                {
                    if (month is < 1 or > 12) return BadRequest("Vælg en gyldig måned.");
                    plan.Month = month;
                }
                if (request.Year is { } year) plan.Year = year;
            }
        }

        var affectedUserIds = await GetAccessibleUserIdsAsync(id, plan.UserId);
        await _context.SaveChangesAsync();
        await _notifier.NotifyPlanChanged(id);
        await _notifier.NotifyPlansChanged(affectedUserIds);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(int id, ActivatePlanRequest request)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null || !IsAccessible(plan)) return NotFound();
        if (plan.IsActive) return Conflict("Planen er allerede aktiveret.");

        plan.IsActive = true;
        if (plan.Type == PlanType.Weekly) plan.ActivatedDate = request.ClientToday.AddDays(1);

        var affectedUserIds = await GetAccessibleUserIdsAsync(id, plan.UserId);
        await _context.SaveChangesAsync();
        await _notifier.NotifyPlanChanged(id);
        await _notifier.NotifyPlansChanged(affectedUserIds);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var plan = await _context.Plans.Where(p => p.UserId == UserId).FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null) return NotFound();

        var affectedUserIds = await GetAccessibleUserIdsAsync(id, plan.UserId);

        _context.PlanTodos.RemoveRange(_context.PlanTodos.Where(t => t.PlanId == id));
        _context.PlanGuests.RemoveRange(_context.PlanGuests.Where(g => g.PlanId == id));
        _context.Plans.Remove(plan);
        await _context.SaveChangesAsync();
        await _notifier.NotifyPlansChanged(affectedUserIds);
        return NoContent();
    }

    [HttpPost("{id}/todos")]
    public async Task<ActionResult<PlanTodoDto>> AddTodo(int id, CreatePlanTodoRequest request)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null || !IsAccessible(plan)) return NotFound();

        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length == 0) return BadRequest("Titel er påkrævet.");

        var maxOrder = await _context.PlanTodos.Where(t => t.PlanId == id).MaxAsync(t => (int?)t.SortOrder);
        var nextOrder = (maxOrder ?? -1) + 1;
        var todo = new PlanTodo
        {
            PlanId = id,
            Title = title,
            DayIndex = plan.Type == PlanType.Weekly ? request.DayIndex : null,
            DayOfMonth = plan.Type == PlanType.Monthly ? request.DayOfMonth : null,
            Time = plan.Type == PlanType.Standard ? null : request.Time,
            SortOrder = nextOrder
        };
        _context.PlanTodos.Add(todo);
        await _context.SaveChangesAsync();

        await _notifier.NotifyPlanChanged(id);
        return ToTodoDto(plan, todo);
    }

    [HttpPatch("{id}/todos/reorder")]
    public async Task<IActionResult> ReorderTodos(int id, ReorderTodosRequest request)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null || !IsAccessible(plan)) return NotFound();

        var todos = await _context.PlanTodos.Where(t => t.PlanId == id && request.TodoIdsInOrder.Contains(t.Id)).ToListAsync();
        var todosById = todos.ToDictionary(t => t.Id);
        for (var i = 0; i < request.TodoIdsInOrder.Count; i++)
        {
            if (todosById.TryGetValue(request.TodoIdsInOrder[i], out var todo)) todo.SortOrder = i;
        }

        await _context.SaveChangesAsync();
        await _notifier.NotifyPlanChanged(id);
        return NoContent();
    }

    [HttpPatch("{id}/todos/{todoId}")]
    public async Task<IActionResult> UpdateTodo(int id, int todoId, UpdatePlanTodoRequest request)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null || !IsAccessible(plan)) return NotFound();

        var todo = await _context.PlanTodos.FirstOrDefaultAsync(t => t.Id == todoId && t.PlanId == id);
        if (todo is null) return NotFound();

        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length == 0) return BadRequest("Titel er påkrævet.");

        todo.Title = title;
        todo.Done = request.Done;
        if (plan.Type == PlanType.Weekly) todo.DayIndex = request.DayIndex;
        if (plan.Type == PlanType.Monthly) todo.DayOfMonth = request.DayOfMonth;
        if (plan.Type != PlanType.Standard) todo.Time = request.Time;

        await _context.SaveChangesAsync();
        await _notifier.NotifyPlanChanged(id);
        return NoContent();
    }

    [HttpDelete("{id}/todos/{todoId}")]
    public async Task<IActionResult> DeleteTodo(int id, int todoId)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null || !IsAccessible(plan)) return NotFound();

        var todo = await _context.PlanTodos.FirstOrDefaultAsync(t => t.Id == todoId && t.PlanId == id);
        if (todo is null) return NotFound();

        _context.PlanTodos.Remove(todo);
        await _context.SaveChangesAsync();
        await _notifier.NotifyPlanChanged(id);
        return NoContent();
    }

    [HttpGet("{id}/guests")]
    public async Task<ActionResult<IEnumerable<GuestDto>>> GetGuests(int id)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == UserId);
        if (plan is null) return NotFound();

        var guestIds = await _context.PlanGuests.Where(g => g.PlanId == id).Select(g => g.UserId).ToListAsync();

        return await _context.Users
            .Where(u => guestIds.Contains(u.Id))
            .Select(u => new GuestDto(u.Id, u.Email!))
            .ToListAsync();
    }

    [HttpPost("{id}/guests")]
    public async Task<ActionResult<GuestDto>> AddGuest(int id, ShareRequest request)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == UserId);
        if (plan is null) return NotFound();

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var guestUser = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (guestUser is null) return NotFound("Ingen bruger fundet med den email.");
        if (guestUser.Id == UserId) return BadRequest("Du kan ikke dele planen med dig selv.");

        var alreadyShared = await _context.PlanGuests.AnyAsync(g => g.PlanId == id && g.UserId == guestUser.Id);
        if (alreadyShared) return Conflict("Brugeren har allerede adgang til planen.");

        _context.PlanGuests.Add(new PlanGuest { PlanId = id, UserId = guestUser.Id });
        await _context.SaveChangesAsync();
        await _notifier.NotifyPlansChanged([guestUser.Id]);
        return new GuestDto(guestUser.Id, guestUser.Email!);
    }

    [HttpDelete("{id}/guests/{guestId}")]
    public async Task<IActionResult> RemoveGuest(int id, string guestId)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == UserId);
        if (plan is null) return NotFound();

        var guest = await _context.PlanGuests.FirstOrDefaultAsync(g => g.PlanId == id && g.UserId == guestId);
        if (guest is not null)
        {
            _context.PlanGuests.Remove(guest);
            await _context.SaveChangesAsync();
            await _notifier.NotifyPlansChanged([guestId]);
        }

        return NoContent();
    }
}
