using Microsoft.EntityFrameworkCore;

public class KnownItemLookupService
{
    private readonly AppDbContext _context;

    public KnownItemLookupService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CategorySuggestion> Suggest(string name, string userId)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0) return new CategorySuggestion(null, null);

        // Aliases are always stored lower-cased (see KnownItemsController), so normalizing the
        // input in .NET (Unicode-correct for æ/ø/å) and comparing with plain "=" avoids relying
        // on SQLite's ASCII-only LOWER().
        var normalized = trimmed.ToLowerInvariant();
        var known = await _context.KnownItemAliases
            .Where(a => a.Alias == normalized)
            .Join(_context.KnownItems, a => a.KnownItemId, k => k.Id, (a, k) => k)
            .FirstOrDefaultAsync();
        if (known is not null) return new CategorySuggestion(known.Category, "Known", known.DefaultUnit);

        // Names keep their original casing (they're shown to the user as typed), so this
        // comparison is done in memory with a culture-correct, case-insensitive check rather
        // than pushing a Unicode-sensitive LOWER() down to SQLite. Shopping items and recipe
        // ingredients share one "have I typed this before" history, since both feed the same
        // known-item catalog.
        var myItems = await _context.ShoppingItems
            .Where(i => i.CreatedByUserId == userId)
            .Select(i => new { i.Name, Category = (string?)i.Category })
            .Distinct()
            .ToListAsync();
        var usedBefore = myItems.FirstOrDefault(i => string.Equals(i.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (usedBefore is not null) return new CategorySuggestion(usedBefore.Category, "History");

        var myIngredients = await _context.RecipeIngredients
            .Where(i => i.CreatedByUserId == userId)
            .Select(i => i.Name)
            .Distinct()
            .ToListAsync();
        var ingredientUsedBefore = myIngredients.Any(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase));
        if (ingredientUsedBefore) return new CategorySuggestion(null, "History");

        return new CategorySuggestion(null, null);
    }

    // Used when stacking ingredients across recipes (meal-plan list generation): resolves a
    // typed name to the catalog's canonical spelling + category when it matches a known alias,
    // so "tomat" and "Tomat" (or any registered alias) group together under one line.
    public async Task<(string CanonicalName, string? Category)> ResolveCanonical(string name)
    {
        var trimmed = name.Trim();
        var normalized = trimmed.ToLowerInvariant();
        var known = await _context.KnownItemAliases
            .Where(a => a.Alias == normalized)
            .Join(_context.KnownItems, a => a.KnownItemId, k => k.Id, (a, k) => k)
            .FirstOrDefaultAsync();
        return known is not null ? (known.CanonicalName, known.Category) : (trimmed, null);
    }
}
