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
    private readonly AppNotifier _notifier;
    private readonly KnownItemLookupService _lookup;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public ShoppingListController(AppDbContext context, AppNotifier notifier, KnownItemLookupService lookup)
    {
        _context = context;
        _notifier = notifier;
        _lookup = lookup;
    }

    private async Task<List<string>> GetAccessibleUserIdsAsync(int listId, string ownerId) =>
        [ownerId, .. await _context.ShoppingListGuests
            .Where(g => g.ShoppingListId == listId)
            .Select(g => g.UserId)
            .ToListAsync()];

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
        var affectedUserIds = await GetAccessibleUserIdsAsync(id, item.UserId);
        await _context.SaveChangesAsync();
        await _notifier.NotifyListsChanged(affectedUserIds);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.ShoppingLists
            .Where(x => x.UserId == UserId)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        var affectedUserIds = await GetAccessibleUserIdsAsync(id, item.UserId);

        var guests = _context.ShoppingListGuests.Where(g => g.ShoppingListId == id);
        _context.ShoppingListGuests.RemoveRange(guests);
        _context.ShoppingLists.Remove(item);
        await _context.SaveChangesAsync();
        await _notifier.NotifyListsChanged(affectedUserIds);
        return NoContent();
    }

    [HttpPost("merge")]
    public async Task<ActionResult<ShoppingListDto>> Merge(MergeListsRequest request)
    {
        var listIds = (request.ListIds ?? []).Distinct().ToList();
        if (listIds.Count < 2) return BadRequest("Vælg mindst to lister at sammenlægge.");

        var lists = await _context.ShoppingLists.Where(l => listIds.Contains(l.Id)).ToListAsync();
        if (lists.Count != listIds.Count || !lists.All(IsAccessible)) return NotFound();

        var guestRows = await _context.ShoppingListGuests.Where(g => listIds.Contains(g.ShoppingListId)).ToListAsync();

        // Everyone who had access to either list keeps access to the merged one, except the
        // merger - they become the new owner rather than a guest of their own list.
        var otherUserIds = lists.Select(l => l.UserId)
            .Concat(guestRows.Select(g => g.UserId))
            .Distinct()
            .Where(uid => uid != UserId)
            .ToList();

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name)) name = string.Join(" + ", lists.Select(l => l.Name));

        var mergedList = new ShoppingList { UserId = UserId, Name = name };
        _context.ShoppingLists.Add(mergedList);
        await _context.SaveChangesAsync();

        foreach (var otherUserId in otherUserIds)
        {
            _context.ShoppingListGuests.Add(new ShoppingListGuest { ShoppingListId = mergedList.Id, UserId = otherUserId });
        }

        // Stack items across the merged lists by canonical name so the same item added
        // separately to both lists collapses into a single row instead of a duplicate.
        var items = await _context.ShoppingItems.Where(i => listIds.Contains(i.ShoppingListId)).ToListAsync();
        var groups = new Dictionary<string, (string Name, string Category, int Quantity)>();
        foreach (var item in items)
        {
            var (canonicalName, _) = await _lookup.ResolveCanonical(item.Name);
            var key = canonicalName.ToLowerInvariant();
            groups.TryGetValue(key, out var existing);
            if (existing.Name is null) existing = (canonicalName, item.Category, 0);
            groups[key] = (existing.Name, existing.Category, existing.Quantity + item.Quantity);
        }
        foreach (var group in groups.Values)
        {
            _context.ShoppingItems.Add(new ShoppingItem
            {
                ShoppingListId = mergedList.Id,
                Name = group.Name,
                Category = group.Category,
                Quantity = group.Quantity,
                CreatedByUserId = UserId
            });
        }

        // The originals are fully absorbed into the merged list, so remove them.
        _context.ShoppingListGuests.RemoveRange(guestRows);
        _context.ShoppingLists.RemoveRange(lists);
        await _context.SaveChangesAsync();

        await _notifier.NotifyListsChanged(otherUserIds.Append(UserId));

        return await ToDto(_context.ShoppingLists.Where(x => x.Id == mergedList.Id)).FirstAsync();
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
        await _notifier.NotifyListsChanged([guestUser.Id]);
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
            await _notifier.NotifyListsChanged([guestId]);
        }

        return NoContent();
    }
}
