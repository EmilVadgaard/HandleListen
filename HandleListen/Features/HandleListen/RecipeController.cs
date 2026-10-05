using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/recipes")]
public class RecipeController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RecipeBookAccessService _bookAccess;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public RecipeController(AppDbContext context, RecipeBookAccessService bookAccess, AppNotifier notifier)
    {
        _context = context;
        _bookAccess = bookAccess;
        _notifier = notifier;
    }

    private Task<int> GetOrCreateMyBookIdAsync() => _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);

    private async Task<List<string>> GetBookMemberIdsAsync(int bookId) =>
        await _context.RecipeBookMembers.Where(m => m.RecipeBookId == bookId).Select(m => m.UserId).ToListAsync();

    private async Task<RecipeDto> ToDtoAsync(Recipe recipe)
    {
        var tags = await _context.RecipeTags
            .Where(t => t.RecipeId == recipe.Id)
            .Select(t => new RecipeTagDto(t.Id, t.Tag))
            .ToListAsync();
        return new RecipeDto(recipe.Id, recipe.Title, recipe.Description, tags);
    }

    [HttpPost]
    public async Task<ActionResult<RecipeDto>> Create(Recipe recipe)
    {
        recipe.RecipeBookId = await GetOrCreateMyBookIdAsync();
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        var dto = await ToDtoAsync(recipe);
        return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, dto);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecipeDto>>> GetAll()
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var recipes = await _context.Recipes
            .Where(r => r.RecipeBookId == bookId)
            .OrderBy(r => r.Title)
            .ToListAsync();
        var recipeIds = recipes.Select(r => r.Id).ToList();
        var tags = await _context.RecipeTags
            .Where(t => recipeIds.Contains(t.RecipeId))
            .ToListAsync();

        return recipes
            .Select(r => new RecipeDto(r.Id, r.Title, r.Description, tags.Where(t => t.RecipeId == r.Id).Select(t => new RecipeTagDto(t.Id, t.Tag)).ToList()))
            .ToList();
    }

    [HttpGet("tags")]
    public async Task<ActionResult<IEnumerable<string>>> GetTagSuggestions()
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var recipeIds = await _context.Recipes.Where(r => r.RecipeBookId == bookId).Select(r => r.Id).ToListAsync();
        var tags = await _context.RecipeTags.Where(t => recipeIds.Contains(t.RecipeId)).Select(t => t.Tag).ToListAsync();

        return tags
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(t => t)
            .ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecipeDto>> GetById(int id)
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var recipe = await _context.Recipes.FirstOrDefaultAsync(x => x.Id == id && x.RecipeBookId == bookId);
        if (recipe is null) return NotFound();
        return await ToDtoAsync(recipe);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateRecipeRequest request)
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var item = await _context.Recipes.FirstOrDefaultAsync(x => x.Id == id && x.RecipeBookId == bookId);
        if (item is null) return NotFound();

        item.Title = request.Title;
        item.Description = request.Description;
        await _context.SaveChangesAsync();
        await _notifier.NotifyRecipesChanged(await GetBookMemberIdsAsync(bookId));
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var item = await _context.Recipes.FirstOrDefaultAsync(x => x.Id == id && x.RecipeBookId == bookId);
        if (item is null) return NotFound();

        var tags = _context.RecipeTags.Where(t => t.RecipeId == id);
        _context.RecipeTags.RemoveRange(tags);
        var ingredients = _context.RecipeIngredients.Where(i => i.RecipeId == id);
        _context.RecipeIngredients.RemoveRange(ingredients);
        _context.Recipes.Remove(item);
        await _context.SaveChangesAsync();
        await _notifier.NotifyRecipesChanged(await GetBookMemberIdsAsync(bookId));
        return NoContent();
    }

    [HttpPost("{id}/tags")]
    public async Task<ActionResult<RecipeTagDto>> AddTag(int id, AddTagRequest request)
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var recipe = await _context.Recipes.FirstOrDefaultAsync(x => x.Id == id && x.RecipeBookId == bookId);
        if (recipe is null) return NotFound();

        var tag = request.Tag?.Trim() ?? string.Empty;
        if (tag.Length == 0) return BadRequest();

        // Compared in memory (not via SQL LOWER()) so æ/ø/å case-folding is Unicode-correct,
        // same reasoning as KnownItemLookupService.
        var existingTags = await _context.RecipeTags.Where(t => t.RecipeId == id).Select(t => t.Tag).ToListAsync();
        if (existingTags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            return Conflict("Taggen findes allerede på denne opskrift.");

        var newTag = new RecipeTag { RecipeId = id, Tag = tag };
        _context.RecipeTags.Add(newTag);
        await _context.SaveChangesAsync();
        await _notifier.NotifyRecipeIngredientsChanged(id);
        return new RecipeTagDto(newTag.Id, newTag.Tag);
    }

    [HttpDelete("{id}/tags/{tagId}")]
    public async Task<IActionResult> RemoveTag(int id, int tagId)
    {
        var bookId = await GetOrCreateMyBookIdAsync();
        var recipe = await _context.Recipes.FirstOrDefaultAsync(x => x.Id == id && x.RecipeBookId == bookId);
        if (recipe is null) return NotFound();

        var tag = await _context.RecipeTags.FirstOrDefaultAsync(t => t.Id == tagId && t.RecipeId == id);
        if (tag is not null)
        {
            _context.RecipeTags.Remove(tag);
            await _context.SaveChangesAsync();
            await _notifier.NotifyRecipeIngredientsChanged(id);
        }

        return NoContent();
    }
}
