using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

        var known = await FindKnownItemAsync(trimmed);
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

    // Used when stacking ingredients across recipes (meal-plan list generation) and when
    // importing a recipe from a link: resolves a typed name to the catalog's canonical spelling
    // + category when it matches a known alias, so "tomat" and "Tomat" (or any registered
    // alias) group together under one line.
    public async Task<(string CanonicalName, string? Category)> ResolveCanonical(string name)
    {
        var trimmed = name.Trim();
        var known = await FindKnownItemAsync(trimmed);
        return known is not null ? (known.CanonicalName, known.Category) : (trimmed, null);
    }

    // Called by the import pipelines (link/AI-text/AI-image import, AI ingredient cleanup) right
    // after a ResolveCanonical call came back with no match (category null) - never from
    // ordinary manual typing elsewhere, which would flood the queue with noise. Repeated misses
    // of the same name (case-insensitive) bump a counter instead of piling up duplicate rows, so
    // Owner can see which gaps in the catalog actually matter.
    public async Task QueueUnmatchedIfNeededAsync(string name, string? category, string requestedByUserId, string? sourceDescription)
    {
        if (category is not null) return;

        var trimmed = name.Trim();
        if (trimmed.Length == 0) return;

        var pending = await _context.KnownItemRequests.Where(r => !r.Resolved).ToListAsync();
        var existing = pending.FirstOrDefault(r => string.Equals(r.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.SeenCount++;
            existing.LastSeenAt = DateTime.UtcNow;
        }
        else
        {
            _context.KnownItemRequests.Add(new KnownItemRequest
            {
                Name = trimmed,
                SourceDescription = sourceDescription,
                RequestedByUserId = requestedByUserId,
            });
        }
        await _context.SaveChangesAsync();
    }

    // Aliases are always stored lower-cased (see KnownItemsController), so normalizing the
    // input in .NET (Unicode-correct for æ/ø/å) and comparing with plain "=" avoids relying on
    // SQLite's ASCII-only LOWER().
    private async Task<KnownItem?> FindKnownItemAsync(string trimmed)
    {
        var normalized = trimmed.ToLowerInvariant();

        var exact = await _context.KnownItemAliases
            .Where(a => a.Alias == normalized)
            .Join(_context.KnownItems, a => a.KnownItemId, k => k.Id, (a, k) => k)
            .FirstOrDefaultAsync();
        if (exact is not null) return exact;

        // Imported or freely-typed names often carry extra descriptive words the catalog
        // doesn't have ("tsk stødt kanel" instead of just "kanel", "frisk persille", "2 fed
        // hakket hvidløg"). If a known alias appears as a whole word (or phrase) inside the
        // text, treat that as a match too - otherwise these would never stack with a plain
        // "kanel"/"persille"/"hvidløg" entry, and wouldn't get the "Known" badge either.
        var allAliases = await _context.KnownItemAliases.ToListAsync();
        var wordMatch = allAliases
            .Where(a => Regex.IsMatch(normalized, $@"(?<!\p{{L}}){Regex.Escape(a.Alias)}(?!\p{{L}})"))
            .OrderByDescending(a => a.Alias.Length)
            .FirstOrDefault();
        if (wordMatch is null) return null;

        return await _context.KnownItems.FirstOrDefaultAsync(k => k.Id == wordMatch.KnownItemId);
    }
}
