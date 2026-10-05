using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/shopping-items")]
public class ShoppingItemController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public ShoppingItemController(AppDbContext context, AppNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingItem>> Create(ShoppingItem item)
    {
        if (item.ShoppingListId == 0)
        {
            item.ShoppingListId = await GetOrCreateDefaultListIdAsync();
        }
        else
        {
            var listAccessible = await _context.ShoppingLists
                .AnyAsync(x => x.Id == item.ShoppingListId &&
                    (x.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == x.Id && g.UserId == UserId)));
            if (!listAccessible) return NotFound();
        }

        item.CreatedByUserId = UserId;
        _context.ShoppingItems.Add(item);
        await _context.SaveChangesAsync();
        await _notifier.NotifyItemsChanged(item.ShoppingListId);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    private async Task<int> GetOrCreateDefaultListIdAsync()
    {
        var existingListId = await _context.ShoppingLists
            .Where(x => x.UserId == UserId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        if (existingListId != 0) return existingListId;

        var list = new ShoppingList { UserId = UserId, Name = "Indkøbsliste" };
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();
        return list.Id;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShoppingItem>>> GetAll()
    {
        return await _context.ShoppingItems
            .Where(x => x.shoppingList.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == x.ShoppingListId && g.UserId == UserId))
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    [HttpGet("by-list/{shoppingListId}")]
    public async Task<ActionResult<IEnumerable<ShoppingItem>>> GetByList(int shoppingListId)
    {
        return await _context.ShoppingItems
            .Where(x => x.ShoppingListId == shoppingListId &&
                (x.shoppingList.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == x.ShoppingListId && g.UserId == UserId)))
            .ToListAsync();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ShoppingItem updatedItem)
    {
        if (id != updatedItem.Id) return BadRequest();

        var item = await _context.ShoppingItems
            .Where(x => x.shoppingList.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == x.ShoppingListId && g.UserId == UserId))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        item.Name = updatedItem.Name;
        item.Category = updatedItem.Category;
        item.Quantity = updatedItem.Quantity;

        await _context.SaveChangesAsync();
        await _notifier.NotifyItemsChanged(item.ShoppingListId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.ShoppingItems
            .Where(x => x.shoppingList.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == x.ShoppingListId && g.UserId == UserId))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        var listId = item.ShoppingListId;
        _context.ShoppingItems.Remove(item);
        await _context.SaveChangesAsync();
        await _notifier.NotifyItemsChanged(listId);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShoppingItem>> GetById(int id)
    {
        var item = await _context.ShoppingItems
            .Where(x => x.shoppingList.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == x.ShoppingListId && g.UserId == UserId))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        return item;
    }

}