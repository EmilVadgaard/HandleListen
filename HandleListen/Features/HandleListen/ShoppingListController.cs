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

    [HttpPost]
    public async Task<ActionResult<ShoppingList>> Create(ShoppingList list)
    {
        list.UserId = UserId;
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = list.Id }, list);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShoppingList>>> GetAll()
    {
        return await _context.ShoppingLists
            .Where(x => x.UserId == UserId)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ShoppingList updatedItem)
    {
        if (id != updatedItem.Id) return BadRequest();

        var item = await _context.ShoppingLists
            .Where(x => x.UserId == UserId)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        item.Name = updatedItem.Name;
        item.GuestIds = updatedItem.GuestIds;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> delete(int id)
    {
        var item = await _context.ShoppingLists
            .Where(x => x.UserId == UserId)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        _context.ShoppingLists.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShoppingList>> GetById(int id)
    {
        var item = await _context.ShoppingLists
            .Where(x => x.UserId == UserId)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        return item;
    }

}