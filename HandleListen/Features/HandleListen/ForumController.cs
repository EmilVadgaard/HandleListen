using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/forum")]
public class ForumController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RecipeBookAccessService _bookAccess;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public ForumController(AppDbContext context, RecipeBookAccessService bookAccess, AppNotifier notifier)
    {
        _context = context;
        _bookAccess = bookAccess;
        _notifier = notifier;
    }

    private async Task<bool> IsRecipeAccessibleAsync(int recipeId)
    {
        var bookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);
        return await _context.Recipes.AnyAsync(r => r.Id == recipeId && r.RecipeBookId == bookId);
    }

    private async Task<SharedRecipeDto> ToDtoAsync(SharedRecipe shared)
    {
        var tags = await _context.SharedRecipeTags.Where(t => t.SharedRecipeId == shared.Id).Select(t => t.Tag).ToListAsync();
        var sharedByUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == shared.SharedByUserId);
        return new SharedRecipeDto(shared.Id, shared.Title, shared.Description, sharedByUser?.Email ?? "", shared.SharedAt, shared.AddedCount, tags, shared.SharedByUserId == UserId);
    }

    // Shares a recipe from the caller's own book as a frozen snapshot - later edits to (or
    // deletion of) the original recipe never change what's already on the forum. Re-sharing the
    // same recipe just returns the existing post instead of creating a duplicate.
    [HttpPost("share")]
    public async Task<ActionResult<SharedRecipeDto>> Share(ShareRecipeRequest request)
    {
        var bookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);
        var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == request.RecipeId && r.RecipeBookId == bookId);
        if (recipe is null) return NotFound();

        var existing = await _context.SharedRecipes.FirstOrDefaultAsync(s => s.OriginalRecipeId == recipe.Id && s.SharedByUserId == UserId);
        if (existing is not null) return await ToDtoAsync(existing);

        var tags = await _context.RecipeTags.Where(t => t.RecipeId == recipe.Id).Select(t => t.Tag).ToListAsync();
        var ingredients = await _context.RecipeIngredients.Where(i => i.RecipeId == recipe.Id).ToListAsync();

        var shared = new SharedRecipe
        {
            Title = recipe.Title,
            Description = recipe.Description,
            SharedByUserId = UserId,
            OriginalRecipeId = recipe.Id,
        };
        _context.SharedRecipes.Add(shared);
        await _context.SaveChangesAsync();

        foreach (var tag in tags)
            _context.SharedRecipeTags.Add(new SharedRecipeTag { SharedRecipeId = shared.Id, Tag = tag });
        foreach (var ingredient in ingredients)
        {
            _context.SharedRecipeIngredients.Add(new SharedRecipeIngredient
            {
                SharedRecipeId = shared.Id,
                Name = ingredient.Name,
                Quantity = ingredient.Quantity,
                Category = ingredient.Category,
                Amount = ingredient.Amount,
                Unit = ingredient.Unit,
            });
        }
        await _context.SaveChangesAsync();

        return await ToDtoAsync(shared);
    }

    [HttpDelete("shared/{id}")]
    public async Task<IActionResult> Unshare(int id)
    {
        var shared = await _context.SharedRecipes.FirstOrDefaultAsync(s => s.Id == id && s.SharedByUserId == UserId);
        if (shared is null) return NotFound();

        _context.SharedRecipes.Remove(shared);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Used by the recipe-card dialog to show the right share-state ("Del til forum" vs.
    // "Fjern fra forum") for one of the caller's own recipes.
    [HttpGet("shared/by-recipe/{recipeId}")]
    public async Task<ActionResult<SharedRecipeDto>> GetByOriginalRecipe(int recipeId)
    {
        if (!await IsRecipeAccessibleAsync(recipeId)) return NotFound();

        var shared = await _context.SharedRecipes.FirstOrDefaultAsync(s => s.OriginalRecipeId == recipeId && s.SharedByUserId == UserId);
        if (shared is null) return NotFound();
        return await ToDtoAsync(shared);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SharedRecipeDto>>> GetAll(string? search)
    {
        var shared = await _context.SharedRecipes.ToListAsync();
        var sharedIds = shared.Select(s => s.Id).ToList();
        var allTags = await _context.SharedRecipeTags.Where(t => sharedIds.Contains(t.SharedRecipeId)).ToListAsync();

        var trimmedSearch = search?.Trim();
        if (!string.IsNullOrEmpty(trimmedSearch))
        {
            // In-memory, Unicode-correct case-insensitive match (same reasoning as
            // KnownItemLookupService) rather than SQLite's ASCII-only LOWER().
            shared = shared.Where(s =>
                s.Title.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase) ||
                allTags.Any(t => t.SharedRecipeId == s.Id && t.Tag.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        var userIds = shared.Select(s => s.SharedByUserId).Distinct().ToList();
        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

        return shared
            .OrderByDescending(s => s.AddedCount)
            .ThenByDescending(s => s.SharedAt)
            .Select(s => new SharedRecipeDto(
                s.Id, s.Title, s.Description,
                users.FirstOrDefault(u => u.Id == s.SharedByUserId)?.Email ?? "",
                s.SharedAt, s.AddedCount,
                allTags.Where(t => t.SharedRecipeId == s.Id).Select(t => t.Tag).ToList(),
                s.SharedByUserId == UserId))
            .ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SharedRecipeDetailDto>> GetById(int id)
    {
        var shared = await _context.SharedRecipes.FirstOrDefaultAsync(s => s.Id == id);
        if (shared is null) return NotFound();

        var tags = await _context.SharedRecipeTags.Where(t => t.SharedRecipeId == id).Select(t => t.Tag).ToListAsync();
        var ingredients = await _context.SharedRecipeIngredients.Where(i => i.SharedRecipeId == id)
            .Select(i => new SharedRecipeIngredientDto(i.Name, i.Quantity, i.Amount, i.Unit, i.Category))
            .ToListAsync();
        var sharedByUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == shared.SharedByUserId);

        return new SharedRecipeDetailDto(shared.Id, shared.Title, shared.Description, sharedByUser?.Email ?? "", shared.SharedAt, shared.AddedCount, tags, ingredients);
    }

    [HttpPost("{id:int}/add")]
    public async Task<ActionResult<RecipeDto>> AddToMyBook(int id)
    {
        var shared = await _context.SharedRecipes.FirstOrDefaultAsync(s => s.Id == id);
        if (shared is null) return NotFound();

        var tags = await _context.SharedRecipeTags.Where(t => t.SharedRecipeId == id).ToListAsync();
        var ingredients = await _context.SharedRecipeIngredients.Where(i => i.SharedRecipeId == id).ToListAsync();

        var bookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);
        var recipe = new Recipe { RecipeBookId = bookId, Title = shared.Title, Description = shared.Description };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        foreach (var tag in tags)
            _context.RecipeTags.Add(new RecipeTag { RecipeId = recipe.Id, Tag = tag.Tag });
        foreach (var ingredient in ingredients)
        {
            _context.RecipeIngredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                Name = ingredient.Name,
                Quantity = ingredient.Quantity,
                Category = ingredient.Category,
                Amount = ingredient.Amount,
                Unit = ingredient.Unit,
                CreatedByUserId = UserId,
            });
        }
        shared.AddedCount++;
        await _context.SaveChangesAsync();

        await _notifier.NotifyRecipesChanged([UserId]);

        var recipeTags = await _context.RecipeTags.Where(t => t.RecipeId == recipe.Id).Select(t => new RecipeTagDto(t.Id, t.Tag)).ToListAsync();
        return new RecipeDto(recipe.Id, recipe.Title, recipe.Description, recipeTags);
    }

    [HttpPost("search-log")]
    public async Task<IActionResult> LogSearch(LogSearchRequest request)
    {
        var query = request.Query?.Trim();
        if (string.IsNullOrEmpty(query)) return NoContent();

        _context.ForumSearchLogs.Add(new ForumSearchLog { Query = query });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("top-searches")]
    public async Task<ActionResult<IEnumerable<TopSearchDto>>> GetTopSearches()
    {
        var queries = await _context.ForumSearchLogs.Select(l => l.Query).ToListAsync();
        return queries
            .GroupBy(q => q.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new TopSearchDto(g.First(), g.Count()))
            .OrderByDescending(t => t.Count)
            .Take(15)
            .ToList();
    }
}
