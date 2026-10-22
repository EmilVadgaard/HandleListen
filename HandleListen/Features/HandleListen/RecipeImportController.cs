using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/recipes/import")]
public class RecipeImportController : ControllerBase
{
    private const long MaxImageBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];

    private readonly RecipeUrlImportService _importService;
    private readonly AiRecipeImportService _aiImportService;
    private readonly UserManager<IdentityUser> _userManager;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public RecipeImportController(RecipeUrlImportService importService, AiRecipeImportService aiImportService, UserManager<IdentityUser> userManager)
    {
        _importService = importService;
        _aiImportService = aiImportService;
        _userManager = userManager;
    }

    [HttpPost("url")]
    public async Task<ActionResult<ImportedRecipeDto>> ImportFromUrl(ImportRecipeFromUrlRequest request)
    {
        var url = request.Url?.Trim();
        if (string.IsNullOrEmpty(url)) return BadRequest();

        var result = await _importService.ImportAsync(url, UserId);
        if (result is null) return NotFound("Kunne ikke finde strukturerede opskriftsdata på denne side.");
        return result;
    }

    [HttpPost("ai-text")]
    public async Task<ActionResult<ImportedRecipeDto>> ImportFromAiText(ImportRecipeFromTextRequest request, CancellationToken ct)
    {
        if (!await CanUseAiImport()) return Forbid();

        var text = request.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return BadRequest();

        var result = await _aiImportService.ImportFromTextAsync(text, UserId, ct);
        if (result is null) return UnprocessableEntity("Kunne ikke finde en opskrift i den indsatte tekst.");
        return result;
    }

    [HttpPost("ai-image")]
    public async Task<ActionResult<ImportedRecipeDto>> ImportFromAiImage(IFormFile file, CancellationToken ct)
    {
        if (!await CanUseAiImport()) return Forbid();

        if (file is null || file.Length == 0) return BadRequest();
        if (file.Length > MaxImageBytes) return BadRequest("Billedet er for stort (maks 10 MB).");
        if (!AllowedImageTypes.Contains(file.ContentType)) return BadRequest("Kun JPEG, PNG og WebP billeder er understøttet.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var result = await _aiImportService.ImportFromImageAsync(stream.ToArray(), file.ContentType, UserId, ct);
        if (result is null) return UnprocessableEntity("Kunne ikke finde en opskrift på billedet.");
        return result;
    }

    [HttpPost("ai-clean-ingredients")]
    public async Task<ActionResult<List<CleanedIngredientDto>>> CleanIngredientNames(CleanIngredientNamesRequest request, CancellationToken ct)
    {
        if (!await CanUseAiImport()) return Forbid();

        if (request.Names is null || request.Names.Count == 0) return BadRequest();

        var result = await _aiImportService.CleanIngredientNamesAsync(request.Names, UserId, ct);
        if (result is null) return UnprocessableEntity("Kunne ikke rette ingredienserne til.");
        return result;
    }

    private async Task<bool> CanUseAiImport()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return false;

        var roles = await _userManager.GetRolesAsync(user);
        return roles.Contains("AiImporter") || roles.Contains("Owner");
    }
}
