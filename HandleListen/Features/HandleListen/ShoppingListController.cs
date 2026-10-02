using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/shopping-lists")]
public class ShoppingListController : ControllerBase
{
    private readonly AppDbContext _context;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public ShoppingListController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAccessible(ShoppingList list) =>
        list.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == list.Id && g.UserId == UserId);

    private IQueryable<ShoppingList> AccessibleListsQuery() =>
        _context.ShoppingLists
            .Where(l => l.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == l.Id && g.UserId == UserId));

    private IQueryable<ShoppingListDto> ToDto(IQueryable<ShoppingList> query) =>
        query.Join(_context.Users, l => l.UserId, u => u.Id,
            (l, u) => new ShoppingListDto(l.Id, l.Name, l.UserId == UserId, u.Email!));

    [HttpPost]
    public async Task<ActionResult<ShoppingListDto>> Create(ShoppingList list)
    {
        list.UserId = UserId;
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        var dto = await ToDto(_context.ShoppingLists.Where(x => x.Id == list.Id)).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = list.Id }, dto);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShoppingListDto>>> GetAll()
    {
        return await ToDto(AccessibleListsQuery().OrderBy(x => x.Name))
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShoppingListDto>> GetById(int id)
    {
        var item = await ToDto(AccessibleListsQuery().Where(x => x.Id == id)).FirstOrDefaultAsync();
        if (item is null) return NotFound();
        return item;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateListRequest request)
    {
        var item = await _context.ShoppingLists.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null || !IsAccessible(item)) return NotFound();

        item.Name = request.Name;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.ShoppingLists
            .Where(x => x.UserId == UserId)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        var guests = _context.ShoppingListGuests.Where(g => g.ShoppingListId == id);
        _context.ShoppingListGuests.RemoveRange(guests);
        _context.ShoppingLists.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/guests")]
    public async Task<ActionResult<IEnumerable<GuestDto>>> GetGuests(int id)
    {
        var list = await _context.ShoppingLists.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (list is null) return NotFound();

        var guestIds = await _context.ShoppingListGuests
            .Where(g => g.ShoppingListId == id)
            .Select(g => g.UserId)
            .ToListAsync();

        return await _context.Users
            .Where(u => guestIds.Contains(u.Id))
            .Select(u => new GuestDto(u.Id, u.Email!))
            .ToListAsync();
    }

    [HttpPost("{id}/guests")]
    public async Task<ActionResult<GuestDto>> AddGuest(int id, ShareRequest request)
    {
        var list = await _context.ShoppingLists.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (list is null) return NotFound();

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var guestUser = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (guestUser is null) return NotFound("Ingen bruger fundet med den email.");
        if (guestUser.Id == UserId) return BadRequest("Du kan ikke dele listen med dig selv.");

        var alreadyShared = await _context.ShoppingListGuests
            .AnyAsync(g => g.ShoppingListId == id && g.UserId == guestUser.Id);
        if (alreadyShared) return Conflict("Brugeren har allerede adgang til listen.");

        _context.ShoppingListGuests.Add(new ShoppingListGuest { ShoppingListId = id, UserId = guestUser.Id });
        await _context.SaveChangesAsync();
        return new GuestDto(guestUser.Id, guestUser.Email!);
    }

    [HttpDelete("{id}/guests/{guestId}")]
    public async Task<IActionResult> RemoveGuest(int id, string guestId)
    {
        var list = await _context.ShoppingLists.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (list is null) return NotFound();

        var guest = await _context.ShoppingListGuests
            .FirstOrDefaultAsync(g => g.ShoppingListId == id && g.UserId == guestId);
        if (guest is not null)
        {
            _context.ShoppingListGuests.Remove(guest);
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }
}
