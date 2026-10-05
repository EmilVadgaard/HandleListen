using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/recipe-ingredients")]
public class RecipeIngredientController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RecipeBookAccessService _bookAccess;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public RecipeIngredientController(AppDbContext context, RecipeBookAccessService bookAccess, AppNotifier notifier)
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

    [HttpGet("by-recipe/{recipeId}")]
    public async Task<ActionResult<IEnumerable<RecipeIngredient>>> GetByRecipe(int recipeId)
    {
        if (!await IsRecipeAccessibleAsync(recipeId)) return NotFound();

        return await _context.RecipeIngredients
            .Where(x => x.RecipeId == recipeId)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<RecipeIngredient>> Create(RecipeIngredient ingredient)
    {
        if (!await IsRecipeAccessibleAsync(ingredient.RecipeId)) return NotFound();

        ingredient.CreatedByUserId = UserId;
        _context.RecipeIngredients.Add(ingredient);
        await _context.SaveChangesAsync();
        await _notifier.NotifyRecipeIngredientsChanged(ingredient.RecipeId);
        return CreatedAtAction(nameof(GetByRecipe), new { recipeId = ingredient.RecipeId }, ingredient);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, RecipeIngredient updated)
    {
        if (id != updated.Id) return BadRequest();

        var ingredient = await _context.RecipeIngredients.FirstOrDefaultAsync(x => x.Id == id);
        if (ingredient is null || !await IsRecipeAccessibleAsync(ingredient.RecipeId)) return NotFound();

        ingredient.Name = updated.Name;
        ingredient.Quantity = updated.Quantity;

        await _context.SaveChangesAsync();
        await _notifier.NotifyRecipeIngredientsChanged(ingredient.RecipeId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ingredient = await _context.RecipeIngredients.FirstOrDefaultAsync(x => x.Id == id);
        if (ingredient is null || !await IsRecipeAccessibleAsync(ingredient.RecipeId)) return NotFound();

        var recipeId = ingredient.RecipeId;
        _context.RecipeIngredients.Remove(ingredient);
        await _context.SaveChangesAsync();
        await _notifier.NotifyRecipeIngredientsChanged(recipeId);
        return NoContent();
    }
}
