using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/known-items")]
public class KnownItemsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly KnownItemLookupService _lookup;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public KnownItemsController(AppDbContext context, KnownItemLookupService lookup)
    {
        _context = context;
        _lookup = lookup;
    }

    [HttpGet("suggest")]
    public async Task<ActionResult<CategorySuggestion>> Suggest([FromQuery] string name)
    {
        return await _lookup.Suggest(name ?? string.Empty, UserId);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<KnownItemDto>>> GetAll([FromQuery] string? search = null)
    {
        var trimmed = search?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return new List<KnownItemDto>();

        var items = await _context.KnownItems
            .OrderBy(k => k.CanonicalName)
            .ToListAsync();
        var aliases = await _context.KnownItemAliases.ToListAsync();

        // Matched in memory (not via SQL LIKE) so æ/ø/å case-folding is Unicode-correct,
        // same reasoning as KnownItemLookupService. Catalogs can grow into the thousands,
        // so results are capped rather than ever shipping the whole table to the client.
        var matchingIds = aliases
            .Where(a => a.Alias.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Select(a => a.KnownItemId)
            .ToHashSet();
        var matching = items
            .Where(k => k.CanonicalName.Contains(trimmed, StringComparison.OrdinalIgnoreCase) || matchingIds.Contains(k.Id))
            .Take(20);

        return matching
            .Select(k => new KnownItemDto(
                k.Id,
                k.CanonicalName,
                k.Category,
                k.DefaultUnit,
                aliases.Where(a => a.KnownItemId == k.Id).Select(a => a.Alias).ToList()))
            .ToList();
    }

    [Authorize(Roles = "Owner,Moderator")]
    [HttpPost]
    public async Task<ActionResult<KnownItemDto>> Create(CreateKnownItemRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var category = request.Category?.Trim() ?? string.Empty;
        if (name.Length == 0 || category.Length == 0) return BadRequest();

        var normalized = name.ToLowerInvariant();
        var exists = await _context.KnownItemAliases.AnyAsync(a => a.Alias == normalized);
        if (exists) return Conflict("Et kendt item med dette navn findes allerede.");

        var item = new KnownItem { CanonicalName = name, Category = category, DefaultUnit = request.DefaultUnit };
        _context.KnownItems.Add(item);
        await _context.SaveChangesAsync();

        _context.KnownItemAliases.Add(new KnownItemAlias { KnownItemId = item.Id, Alias = normalized });
        await _context.SaveChangesAsync();

        return new KnownItemDto(item.Id, item.CanonicalName, item.Category, item.DefaultUnit, new List<string> { normalized });
    }

    [Authorize(Roles = "Owner,Moderator")]
    [HttpPost("{id}/aliases")]
    public async Task<IActionResult> AddAlias(int id, AddAliasRequest request)
    {
        var knownItem = await _context.KnownItems.FindAsync(id);
        if (knownItem is null) return NotFound();

        var alias = request.Alias?.Trim() ?? string.Empty;
        if (alias.Length == 0) return BadRequest();

        var normalized = alias.ToLowerInvariant();
        var exists = await _context.KnownItemAliases.AnyAsync(a => a.Alias == normalized);
        if (exists) return Conflict("Dette navn er allerede registreret.");

        _context.KnownItemAliases.Add(new KnownItemAlias { KnownItemId = id, Alias = normalized });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Admin manager (see AdminController) - browse/edit/delete the whole catalog. Owner,Moderator
    // like the rest of this controller's mutating endpoints, not Owner-only like AdminController's
    // request queue, since curating known items has always been a shared Owner/Moderator job.
    [Authorize(Roles = "Owner,Moderator")]
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<KnownItemAdminDto>>> GetAllForAdmin()
    {
        var items = await _context.KnownItems.OrderBy(k => k.CanonicalName).ToListAsync();
        var aliases = await _context.KnownItemAliases.ToListAsync();

        return items
            .Select(k => new KnownItemAdminDto(
                k.Id, k.CanonicalName, k.Category, k.DefaultUnit,
                aliases.Where(a => a.KnownItemId == k.Id).Select(a => new KnownItemAliasDto(a.Id, a.Alias)).ToList()))
            .ToList();
    }

    [Authorize(Roles = "Owner,Moderator")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateKnownItemRequest request)
    {
        var item = await _context.KnownItems.FindAsync(id);
        if (item is null) return NotFound();

        var name = request.CanonicalName?.Trim() ?? string.Empty;
        var category = request.Category?.Trim() ?? string.Empty;
        if (name.Length == 0 || category.Length == 0) return BadRequest();

        item.CanonicalName = name;
        item.Category = category;
        item.DefaultUnit = request.DefaultUnit;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Owner,Moderator")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.KnownItems.FindAsync(id);
        if (item is null) return NotFound();

        _context.KnownItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Owner,Moderator")]
    [HttpDelete("{id}/aliases/{aliasId}")]
    public async Task<IActionResult> RemoveAlias(int id, int aliasId)
    {
        var remainingCount = await _context.KnownItemAliases.CountAsync(a => a.KnownItemId == id);
        if (remainingCount <= 1) return BadRequest("Et kendt item skal have mindst ét navn - slet hele kendt item i stedet.");

        var alias = await _context.KnownItemAliases.FirstOrDefaultAsync(a => a.Id == aliasId && a.KnownItemId == id);
        if (alias is null) return NotFound();

        _context.KnownItemAliases.Remove(alias);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
