using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ShoppingItemController : ControllerBase
{
    private readonly AppDbContext _context;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public ShoppingItemController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingItem>> Create(ShoppingItem item)
    {
        item.UserId = UserId;
        _context.ShoppingItems.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShoppingItem>>> GetAll()
    {
        return await _context.ShoppingItems
            .Where(x => x.UserId == UserId)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ShoppingItem updatedItem)
    {
        if (id != updatedItem.Id) return BadRequest();

        var item = await _context.ShoppingItems
            .Where(x => x.UserId == UserId)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        item.Name = updatedItem.Name;
        item.Category = updatedItem.Category;
        item.Quantity = updatedItem.Quantity;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> delete(int id)
    {
        var item = await _context.ShoppingItems
            .Where(x => x.UserId == UserId)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        _context.ShoppingItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShoppingItem>> GetById(int id)
    {
        var item = await _context.ShoppingItems
            .Where(x => x.UserId == UserId)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        return item;
    }

}