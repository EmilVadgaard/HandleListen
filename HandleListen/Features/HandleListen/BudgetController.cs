using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/budget")]
public class BudgetController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly BudgetAccessService _budgetAccess;
    private readonly AppNotifier _notifier;
    private readonly BlockService _blockService;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public BudgetController(AppDbContext context, BudgetAccessService budgetAccess, AppNotifier notifier, BlockService blockService)
    {
        _context = context;
        _budgetAccess = budgetAccess;
        _notifier = notifier;
        _blockService = blockService;
    }

    private async Task<List<string>> GetMemberIdsAsync(int budgetId) =>
        await _context.BudgetMembers.Where(m => m.BudgetId == budgetId).Select(m => m.UserId).ToListAsync();

    [HttpGet]
    public async Task<ActionResult<BudgetIdDto>> GetMine()
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        return new BudgetIdDto(budgetId);
    }

    [HttpPost("leave")]
    public async Task<IActionResult> Leave()
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);

        var otherMemberIds = await _context.BudgetMembers
            .Where(m => m.BudgetId == budgetId && m.UserId != UserId)
            .Select(m => m.UserId)
            .ToListAsync();
        if (otherMemberIds.Count == 0) return BadRequest("Du deler ikke dit budget med nogen.");

        var myMember = await _context.BudgetMembers
            .FirstAsync(m => m.BudgetId == budgetId && m.UserId == UserId);

        var newBudget = new Budget();
        _context.Budgets.Add(newBudget);
        await _context.SaveChangesAsync();

        var existingFixedEntries = await _context.BudgetFixedEntries.Where(e => e.BudgetId == budgetId).ToListAsync();
        foreach (var e in existingFixedEntries)
        {
            _context.BudgetFixedEntries.Add(new BudgetFixedEntry { BudgetId = newBudget.Id, Name = e.Name, Type = e.Type, Amount = e.Amount });
        }

        var existingVariableCategories = await _context.BudgetVariableCategories.Where(c => c.BudgetId == budgetId).ToListAsync();
        foreach (var c in existingVariableCategories)
        {
            _context.BudgetVariableCategories.Add(new BudgetVariableCategory
            {
                BudgetId = newBudget.Id,
                Name = c.Name,
                Type = c.Type,
                ExpectedAmount = c.ExpectedAmount,
                ActualAmount = c.ActualAmount
            });
        }

        myMember.BudgetId = newBudget.Id;
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged(otherMemberIds.Append(UserId));
        return NoContent();
    }

    [HttpGet("members")]
    public async Task<ActionResult<IEnumerable<BudgetMemberDto>>> GetMembers()
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        return await _context.BudgetMembers
            .Where(m => m.BudgetId == budgetId)
            .Join(_context.Users, m => m.UserId, u => u.Id, (m, u) => new BudgetMemberDto(u.Id, u.Email!))
            .ToListAsync();
    }

    [HttpGet("invites")]
    public async Task<ActionResult<IEnumerable<BudgetInviteDto>>> GetInvites()
    {
        var invites = await _context.BudgetInvites
            .Where(i => i.FromUserId == UserId || i.ToUserId == UserId)
            .ToListAsync();

        var userIds = invites.SelectMany(i => new[] { i.FromUserId, i.ToUserId }).Distinct().ToList();
        var emails = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email!);

        return invites
            .Select(i => new BudgetInviteDto(
                i.Id,
                emails.GetValueOrDefault(i.FromUserId, ""),
                emails.GetValueOrDefault(i.ToUserId, ""),
                i.ToUserId == UserId,
                i.CreatedAt))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }

    [HttpPost("invites")]
    public async Task<ActionResult<BudgetInviteDto>> CreateInvite(InviteRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var toUser = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (toUser is null) return NotFound("Ingen bruger fundet med den email.");
        if (toUser.Id == UserId) return BadRequest("Du kan ikke invitere dig selv.");
        if (await _blockService.HasBlockedAsync(toUser.Id, UserId)) return NotFound("Ingen bruger fundet med den email.");

        var myBudgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        var theirBudgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(toUser.Id);
        if (myBudgetId == theirBudgetId) return Conflict("I deler allerede budget.");

        var alreadyInvited = await _context.BudgetInvites.AnyAsync(i =>
            (i.FromUserId == UserId && i.ToUserId == toUser.Id) ||
            (i.FromUserId == toUser.Id && i.ToUserId == UserId));
        if (alreadyInvited) return Conflict("Der findes allerede en invitation mellem jer.");

        var invite = new BudgetInvite { FromUserId = UserId, ToUserId = toUser.Id };
        _context.BudgetInvites.Add(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged([toUser.Id]);

        var myEmail = await _context.Users.Where(u => u.Id == UserId).Select(u => u.Email!).FirstAsync();
        return new BudgetInviteDto(invite.Id, myEmail, toUser.Email!, false, invite.CreatedAt);
    }

    [HttpPost("invites/{id}/accept")]
    public async Task<IActionResult> AcceptInvite(int id)
    {
        var invite = await _context.BudgetInvites.FirstOrDefaultAsync(i => i.Id == id && i.ToUserId == UserId);
        if (invite is null) return NotFound();

        var fromBudgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(invite.FromUserId);
        var toBudgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);

        var affectedUserIds = new List<string>();

        if (fromBudgetId != toBudgetId)
        {
            var members = await _context.BudgetMembers
                .Where(m => m.BudgetId == fromBudgetId || m.BudgetId == toBudgetId)
                .ToListAsync();
            affectedUserIds.AddRange(members.Select(m => m.UserId));

            var mergedBudget = new Budget();
            _context.Budgets.Add(mergedBudget);
            await _context.SaveChangesAsync();

            var fixedEntries = await _context.BudgetFixedEntries
                .Where(e => e.BudgetId == fromBudgetId || e.BudgetId == toBudgetId)
                .ToListAsync();
            foreach (var e in fixedEntries) e.BudgetId = mergedBudget.Id;

            var variableCategories = await _context.BudgetVariableCategories
                .Where(c => c.BudgetId == fromBudgetId || c.BudgetId == toBudgetId)
                .ToListAsync();
            foreach (var c in variableCategories) c.BudgetId = mergedBudget.Id;

            foreach (var member in members) member.BudgetId = mergedBudget.Id;

            await _context.SaveChangesAsync();

            var oldBudgets = await _context.Budgets
                .Where(b => b.Id == fromBudgetId || b.Id == toBudgetId)
                .ToListAsync();
            _context.Budgets.RemoveRange(oldBudgets);
        }

        _context.BudgetInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged(affectedUserIds);
        return NoContent();
    }

    [HttpPost("invites/{id}/decline")]
    public async Task<IActionResult> DeclineInvite(int id)
    {
        var invite = await _context.BudgetInvites.FirstOrDefaultAsync(i => i.Id == id && i.ToUserId == UserId);
        if (invite is null) return NotFound();

        _context.BudgetInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged([invite.FromUserId]);
        return NoContent();
    }

    [HttpDelete("invites/{id}")]
    public async Task<IActionResult> CancelInvite(int id)
    {
        var invite = await _context.BudgetInvites.FirstOrDefaultAsync(i => i.Id == id && i.FromUserId == UserId);
        if (invite is null) return NotFound();

        _context.BudgetInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged([invite.ToUserId]);
        return NoContent();
    }

    [HttpGet("fixed-entries")]
    public async Task<ActionResult<IEnumerable<BudgetFixedEntryDto>>> GetFixedEntries()
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        return await _context.BudgetFixedEntries
            .Where(e => e.BudgetId == budgetId)
            .OrderBy(e => e.Id)
            .Select(e => new BudgetFixedEntryDto(e.Id, e.Name, e.Type, e.Amount))
            .ToListAsync();
    }

    [HttpPost("fixed-entries")]
    public async Task<ActionResult<BudgetFixedEntryDto>> CreateFixedEntry(CreateBudgetFixedEntryRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0) return BadRequest("Navn er påkrævet.");
        if (request.Amount < 0) return BadRequest("Beløb kan ikke være negativt.");

        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        var entry = new BudgetFixedEntry { BudgetId = budgetId, Name = name, Type = request.Type, Amount = request.Amount };
        _context.BudgetFixedEntries.Add(entry);
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged(await GetMemberIdsAsync(budgetId));
        return new BudgetFixedEntryDto(entry.Id, entry.Name, entry.Type, entry.Amount);
    }

    [HttpPut("fixed-entries/{id}")]
    public async Task<IActionResult> UpdateFixedEntry(int id, UpdateBudgetFixedEntryRequest request)
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        var entry = await _context.BudgetFixedEntries.FirstOrDefaultAsync(e => e.Id == id && e.BudgetId == budgetId);
        if (entry is null) return NotFound();

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0) return BadRequest("Navn er påkrævet.");
        if (request.Amount < 0) return BadRequest("Beløb kan ikke være negativt.");

        entry.Name = name;
        entry.Type = request.Type;
        entry.Amount = request.Amount;
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged(await GetMemberIdsAsync(budgetId));
        return NoContent();
    }

    [HttpDelete("fixed-entries/{id}")]
    public async Task<IActionResult> DeleteFixedEntry(int id)
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        var entry = await _context.BudgetFixedEntries.FirstOrDefaultAsync(e => e.Id == id && e.BudgetId == budgetId);
        if (entry is null) return NotFound();

        _context.BudgetFixedEntries.Remove(entry);
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged(await GetMemberIdsAsync(budgetId));
        return NoContent();
    }

    [HttpGet("variable-categories")]
    public async Task<ActionResult<IEnumerable<BudgetVariableCategoryDto>>> GetVariableCategories()
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        return await _context.BudgetVariableCategories
            .Where(c => c.BudgetId == budgetId)
            .OrderBy(c => c.Id)
            .Select(c => new BudgetVariableCategoryDto(c.Id, c.Name, c.Type, c.ExpectedAmount, c.ActualAmount))
            .ToListAsync();
    }

    [HttpPost("variable-categories")]
    public async Task<ActionResult<BudgetVariableCategoryDto>> CreateVariableCategory(CreateBudgetVariableCategoryRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0) return BadRequest("Navn er påkrævet.");
        if (request.ExpectedAmount < 0 || request.ActualAmount < 0) return BadRequest("Beløb kan ikke være negativt.");

        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        var category = new BudgetVariableCategory
        {
            BudgetId = budgetId,
            Name = name,
            Type = request.Type,
            ExpectedAmount = request.ExpectedAmount,
            ActualAmount = request.ActualAmount
        };
        _context.BudgetVariableCategories.Add(category);
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged(await GetMemberIdsAsync(budgetId));
        return new BudgetVariableCategoryDto(category.Id, category.Name, category.Type, category.ExpectedAmount, category.ActualAmount);
    }

    [HttpPut("variable-categories/{id}")]
    public async Task<IActionResult> UpdateVariableCategory(int id, UpdateBudgetVariableCategoryRequest request)
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        var category = await _context.BudgetVariableCategories.FirstOrDefaultAsync(c => c.Id == id && c.BudgetId == budgetId);
        if (category is null) return NotFound();

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0) return BadRequest("Navn er påkrævet.");
        if (request.ExpectedAmount < 0 || request.ActualAmount < 0) return BadRequest("Beløb kan ikke være negativt.");

        category.Name = name;
        category.Type = request.Type;
        category.ExpectedAmount = request.ExpectedAmount;
        category.ActualAmount = request.ActualAmount;
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged(await GetMemberIdsAsync(budgetId));
        return NoContent();
    }

    [HttpDelete("variable-categories/{id}")]
    public async Task<IActionResult> DeleteVariableCategory(int id)
    {
        var budgetId = await _budgetAccess.GetOrCreateBudgetIdAsync(UserId);
        var category = await _context.BudgetVariableCategories.FirstOrDefaultAsync(c => c.Id == id && c.BudgetId == budgetId);
        if (category is null) return NotFound();

        _context.BudgetVariableCategories.Remove(category);
        await _context.SaveChangesAsync();

        await _notifier.NotifyBudgetChanged(await GetMemberIdsAsync(budgetId));
        return NoContent();
    }
}
