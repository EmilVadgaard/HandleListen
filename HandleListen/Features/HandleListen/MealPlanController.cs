using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/meal-plans")]
public class MealPlanController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RecipeBookAccessService _bookAccess;
    private readonly KnownItemLookupService _lookup;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public MealPlanController(AppDbContext context, RecipeBookAccessService bookAccess, KnownItemLookupService lookup, AppNotifier notifier)
    {
        _context = context;
        _bookAccess = bookAccess;
        _lookup = lookup;
        _notifier = notifier;
    }

    private Task<int> GetOrCreateMyBookIdAsync() => _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);

    private async Task<List<string>> GetBookMemberIdsAsync(int bookId) =>
        await _context.RecipeBookMembers.Where(m => m.RecipeBookId == bookId).Select(m => m.UserId).ToListAsync();

    private async Task<MealPlanDto> ToDtoAsync(MealPlan plan)
    {
        var recipes = await _context.MealPlanRecipes
            .Where(mpr => mpr.MealPlanId == plan.Id)
            .Join(_context.Recipes, mpr => mpr.RecipeId, r => r.Id, (mpr, r) => new RecipeSummaryDto(r.Id, r.Title))
            .ToListAsync();
        return new MealPlanDto(plan.Id, plan.Name, recipes);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MealPlanDto>>> GetAll()
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var plans = await _context.MealPlans
            .Where(p => p.RecipeBookId == bookId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var dtos = new List<MealPlanDto>();
        foreach (var plan in plans) dtos.Add(await ToDtoAsync(plan));
        return dtos;
    }

    [HttpPost]
    public async Task<ActionResult<MealPlanDto>> Create(CreateMealPlanRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0) return BadRequest();

        var bookId = await GetOrCreateMyBookIdAsync();
        var validRecipeIds = await _context.Recipes
            .Where(r => r.RecipeBookId == bookId && request.RecipeIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        var plan = new MealPlan { RecipeBookId = bookId, Name = name };
        _context.MealPlans.Add(plan);
        await _context.SaveChangesAsync();

        foreach (var recipeId in validRecipeIds)
        {
            _context.MealPlanRecipes.Add(new MealPlanRecipe { MealPlanId = plan.Id, RecipeId = recipeId });
        }
        await _context.SaveChangesAsync();

        await _notifier.NotifyMealPlansChanged(await GetBookMemberIdsAsync(bookId));
        return await ToDtoAsync(plan);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var plan = await _context.MealPlans.FirstOrDefaultAsync(p => p.Id == id && p.RecipeBookId == bookId);
        if (plan is null) return NotFound();

        var links = _context.MealPlanRecipes.Where(mpr => mpr.MealPlanId == id);
        _context.MealPlanRecipes.RemoveRange(links);
        _context.MealPlans.Remove(plan);
        await _context.SaveChangesAsync();

        await _notifier.NotifyMealPlansChanged(await GetBookMemberIdsAsync(bookId));
        return NoContent();
    }

    [HttpGet("{id}/generate-preview")]
    public async Task<ActionResult<IEnumerable<GeneratedListItemDto>>> GeneratePreview(int id)
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var plan = await _context.MealPlans.FirstOrDefaultAsync(p => p.Id == id && p.RecipeBookId == bookId);
        if (plan is null) return NotFound();

        var recipeIds = await _context.MealPlanRecipes
            .Where(mpr => mpr.MealPlanId == id)
            .Select(mpr => mpr.RecipeId)
            .ToListAsync();
        var ingredients = await _context.RecipeIngredients
            .Where(i => recipeIds.Contains(i.RecipeId))
            .ToListAsync();

        var groups = new Dictionary<string, (string Name, string? Category, int Quantity, decimal Grams, bool HasGrams, decimal Milliliters, bool HasMilliliters, decimal Pieces, bool HasPieces)>();

        foreach (var ingredient in ingredients)
        {
            var (canonicalName, catalogCategory) = await _lookup.ResolveCanonical(ingredient.Name);
            // An explicit category chosen on the ingredient itself always wins over the
            // central catalog's guess - that's the whole point of being able to set one.
            var resolvedCategory = !string.IsNullOrWhiteSpace(ingredient.Category) ? ingredient.Category.Trim() : catalogCategory;
            var key = canonicalName.ToLowerInvariant();

            groups.TryGetValue(key, out var existing);
            if (existing.Name is null)
            {
                existing = (canonicalName, resolvedCategory, 0, 0, false, 0, false, 0, false);
            }

            var quantity = existing.Quantity + ingredient.Quantity;
            var grams = existing.Grams;
            var hasGrams = existing.HasGrams;
            var milliliters = existing.Milliliters;
            var hasMilliliters = existing.HasMilliliters;
            var pieces = existing.Pieces;
            var hasPieces = existing.HasPieces;

            if (ingredient.Amount is decimal amount && ingredient.Unit is UnitOfMeasure unit)
            {
                switch (unit)
                {
                    case UnitOfMeasure.Gram:
                        grams += amount;
                        hasGrams = true;
                        break;
                    case UnitOfMeasure.Kilogram:
                        grams += amount * 1000;
                        hasGrams = true;
                        break;
                    case UnitOfMeasure.Milliliter:
                        milliliters += amount;
                        hasMilliliters = true;
                        break;
                    case UnitOfMeasure.Deciliter:
                        milliliters += amount * 100;
                        hasMilliliters = true;
                        break;
                    case UnitOfMeasure.Liter:
                        milliliters += amount * 1000;
                        hasMilliliters = true;
                        break;
                    case UnitOfMeasure.Piece:
                        pieces += amount;
                        hasPieces = true;
                        break;
                }
            }

            groups[key] = (existing.Name, existing.Category ?? resolvedCategory, quantity, grams, hasGrams, milliliters, hasMilliliters, pieces, hasPieces);
        }

        return groups.Values
            .Select(g => new GeneratedListItemDto(g.Name, g.Category ?? "Diverse", g.Quantity, FormatAmountSummary(g.Grams, g.HasGrams, g.Milliliters, g.HasMilliliters, g.Pieces, g.HasPieces)))
            .OrderBy(g => g.Category).ThenBy(g => g.Name)
            .ToList();
    }

    private static string? FormatAmountSummary(decimal grams, bool hasGrams, decimal milliliters, bool hasMilliliters, decimal pieces, bool hasPieces)
    {
        var parts = new List<string>();
        if (hasGrams) parts.Add(FormatAmount(grams, "g", "kg"));
        if (hasMilliliters) parts.Add(FormatAmount(milliliters, "ml", "l"));
        if (hasPieces) parts.Add($"{FormatNumber(pieces)} stk");
        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    private static string FormatAmount(decimal baseValue, string smallUnit, string bigUnit)
    {
        if (baseValue >= 1000)
        {
            return $"{FormatNumber(baseValue / 1000)} {bigUnit}";
        }
        return $"{FormatNumber(baseValue)} {smallUnit}";
    }

    private static string FormatNumber(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    [HttpPost("{id}/generate")]
    public async Task<ActionResult<ShoppingListDto>> Generate(int id, GenerateListRequest request)
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var plan = await _context.MealPlans.FirstOrDefaultAsync(p => p.Id == id && p.RecipeBookId == bookId);
        if (plan is null) return NotFound();

        var listName = request.ListName?.Trim();
        if (string.IsNullOrEmpty(listName)) listName = plan.Name;

        var list = new ShoppingList { UserId = UserId, Name = listName };
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            var name = item.Name?.Trim();
            if (string.IsNullOrEmpty(name) || item.Quantity < 1) continue;

            _context.ShoppingItems.Add(new ShoppingItem
            {
                ShoppingListId = list.Id,
                Name = name,
                Category = item.Category?.Trim() is { Length: > 0 } cat ? cat : "Diverse",
                Quantity = item.Quantity,
                CreatedByUserId = UserId
            });
        }
        await _context.SaveChangesAsync();

        await _notifier.NotifyListsChanged([UserId]);

        return new ShoppingListDto(list.Id, list.Name, true, (await _context.Users.FirstAsync(u => u.Id == UserId)).Email!);
    }
}
