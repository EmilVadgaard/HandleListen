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
}
