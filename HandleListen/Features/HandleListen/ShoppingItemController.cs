using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ShoppingItemController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShoppingItemController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingItem>> Create(ShoppingItem item)
    {
        _context.ShoppingItems.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShoppingItem>>> GetAll()
    {
        return await _context.ShoppingItems
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ShoppingItem updatedItem)
    {
        if (id != updatedItem.Id) return BadRequest();

        var item = await _context.ShoppingItems.FindAsync(id);
        if (item is null) return NotFound();

        item.Name = updatedItem.Name;
        item.Quantity = updatedItem.Quantity;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> delete(int id)
    {
        var item = await _context.ShoppingItems.FindAsync(id);
        if (item is null) return NotFound();

        _context.ShoppingItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShoppingItem>> GetById(int id)
    {
        var item = await _context.ShoppingItems.FindAsync(id);
        if (item == null) return NotFound();
        return item;
    }

}