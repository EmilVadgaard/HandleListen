using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

// Reads a recipe out of a photo or pasted free text using Claude's vision + structured-output
// support - unlike RecipeUrlImportService there's no structured schema.org data to lean on here,
// so this is the one part of recipe import that actually needs AI. Deliberately gated behind the
// "AiImporter" role (see RecipeImportController) since every call costs real money and is the one
// feature in this app with an open-ended cost if usage grew unexpectedly.
public class AiRecipeImportService
{
    private const string Model = "claude-haiku-5-5";

    private const string Instructions =
        "Du udtrækker en opskrift fra enten et foto af en opskrift eller indsat ren tekst. " +
        "Svar udelukkende med strukturerede data på dansk. For hver ingrediens: 'name' er selve " +
        "ingrediensen uden foranstillede mængdeord (f.eks. 'kanel', ikke 'tsk stødt kanel'). " +
        "'quantity' er et heltal >= 1 og betyder 'hvor mange skal købes' - brug kun > 1 når " +
        "opskriften tæller styks uden vægt/volumen (f.eks. '2 løg' -> quantity 2). 'amount' er en " +
        "decimal mængde i den angivne enhed (f.eks. 400 for '400 g'), eller null hvis ingen mængde " +
        "er angivet. 'unit' er en af gram, kilogram, milliliter, deciliter, liter, piece, none - " +
        "brug 'none' hvis ingen enhed passer. 'description' er fremgangsmåden som en nummereret " +
        "liste, ét trin per linje. Hvis billedet/teksten ikke indeholder en opskrift, sæt title " +
        "til en tom streng.";

    private static readonly Dictionary<string, JsonElement> ResponseSchema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
        """
        {
          "type": "object",
          "properties": {
            "title": { "type": "string" },
            "description": { "type": "string" },
            "ingredients": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "name": { "type": "string" },
                  "quantity": { "type": "integer" },
                  "amount": { "type": ["number", "null"] },
                  "unit": { "type": "string", "enum": ["gram", "kilogram", "milliliter", "deciliter", "liter", "piece", "none"] }
                },
                "required": ["name", "quantity", "amount", "unit"],
                "additionalProperties": false
              }
            }
          },
          "required": ["title", "description", "ingredients"],
          "additionalProperties": false
        }
        """)!;

    private const string CleanNamesInstructions =
        "Du renser navne på opskrifts-ingredienser til det der faktisk skal købes/findes i " +
        "butikken, på dansk. Fjern købs-antal, enheds-/beholderord (stk, dåse, pakke, bundt osv.) " +
        "og tilberednings-/serveringsinstrukser der ikke ændrer hvilket produkt man køber (f.eks. " +
        "'drænet og skyllet', 'skåret i tern', 'til servering', 'hakket' når det bare er en " +
        "tilberedningsmetode). BEVAR ord der angiver hvilken variant af produktet man skal købe " +
        "(f.eks. 'stødt' i 'stødt kanel' betyder malet kanel i modsætning til kanelstænger - det " +
        "er en anden vare, ikke en tilberedningsmetode; bevar også 'frisk' vs. 'frossen', 'øko' " +
        "osv.). Store forbogstaver kun på egennavne, ellers små bogstaver. Eksempler: " +
        "'stk stødt kanel' -> 'stødt kanel'. 'dåser kidney bønner, drænede og skyllede' -> " +
        "'kidney bønner'. Returnér én renset navn per indgående indeks, i samme antal som input.";

    private static readonly Dictionary<string, JsonElement> CleanNamesResponseSchema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
        """
        {
          "type": "object",
          "properties": {
            "names": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "index": { "type": "integer" },
                  "name": { "type": "string" }
                },
                "required": ["index", "name"],
                "additionalProperties": false
              }
            }
          },
          "required": ["names"],
          "additionalProperties": false
        }
        """)!;

    private readonly AnthropicClient? _client;
    private readonly KnownItemLookupService _lookup;
    private readonly ILogger<AiRecipeImportService> _logger;

    public AiRecipeImportService(IConfiguration configuration, KnownItemLookupService lookup, ILogger<AiRecipeImportService> logger)
    {
        var apiKey = configuration["Anthropic:ApiKey"];
        _client = string.IsNullOrEmpty(apiKey) ? null : new AnthropicClient { ApiKey = apiKey };
        _lookup = lookup;
        _logger = logger;
    }

    // "AI Ret" button in the import review screen - cleans up ingredient names the way the AI
    // extraction (or a messy pasted/typed name) left them, then re-resolves each through the
    // known-item catalog so a now-cleaner name like "kidney bønner" picks up the right
    // canonical spelling/category if the catalog has it.
    public async Task<List<CleanedIngredientDto>?> CleanIngredientNamesAsync(List<string> names, string requestedByUserId, CancellationToken ct)
    {
        if (_client is null)
        {
            _logger.LogWarning("Anthropic is not configured - ingredient name cleanup was not run.");
            return null;
        }
        if (names.Count == 0) return [];

        var numbered = string.Join("\n", names.Select((n, i) => $"{i}: {n}"));
        Message response;
        try
        {
            response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = Model,
                MaxTokens = 4096,
                System = CleanNamesInstructions,
                OutputConfig = new OutputConfig
                {
                    Format = new JsonOutputFormat { Schema = CleanNamesResponseSchema },
                },
                Messages = [new() { Role = Role.User, Content = numbered }],
            }, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI ingredient cleanup request to Anthropic failed");
            return null;
        }

        var json = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(json)) return null;

        var parsed = JsonSerializer.Deserialize<CleanedNamesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (parsed is null) return null;

        var cleanedByIndex = parsed.Names.ToDictionary(n => n.Index, n => n.Name);
        var result = new List<CleanedIngredientDto>();
        for (var i = 0; i < names.Count; i++)
        {
            var cleanedName = cleanedByIndex.TryGetValue(i, out var name) && !string.IsNullOrWhiteSpace(name) ? name : names[i];
            var (canonicalName, category) = await _lookup.ResolveCanonical(cleanedName);
            await _lookup.QueueUnmatchedIfNeededAsync(cleanedName, category, requestedByUserId, "AI Ret (ingrediens-oprydning)");
            result.Add(new CleanedIngredientDto(canonicalName, category));
        }

        return result;
    }

    public Task<ImportedRecipeDto?> ImportFromTextAsync(string text, string requestedByUserId, CancellationToken ct) =>
        RunAsync([new TextBlockParam { Text = text }], requestedByUserId, "AI-tekst-import", ct);

    public Task<ImportedRecipeDto?> ImportFromImageAsync(byte[] imageBytes, string mediaType, string requestedByUserId, CancellationToken ct) =>
        RunAsync(
        [
            new ImageBlockParam { Source = new Base64ImageSource { MediaType = mediaType, Data = Convert.ToBase64String(imageBytes) } },
            new TextBlockParam { Text = "Her er et foto af en opskrift." },
        ], requestedByUserId, "AI-billede-import", ct);

    private async Task<ImportedRecipeDto?> RunAsync(List<ContentBlockParam> content, string requestedByUserId, string sourceDescription, CancellationToken ct)
    {
        if (_client is null)
        {
            _logger.LogWarning("Anthropic is not configured - AI recipe import was not run.");
            return null;
        }

        Message response;
        try
        {
            response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = Model,
                MaxTokens = 4096,
                System = Instructions,
                OutputConfig = new OutputConfig
                {
                    Format = new JsonOutputFormat { Schema = ResponseSchema },
                },
                Messages = [new() { Role = Role.User, Content = content }],
            }, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // Covers both transport failures and Anthropic rejecting the request (e.g. an
            // invalid schema) - logged in full so a bad request shows the real reason instead
            // of silently landing on the generic "no recipe found" message the caller falls
            // back to for every other null-returning case.
            _logger.LogError(ex, "AI recipe import request to Anthropic failed");
            return null;
        }

        var json = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(json)) return null;

        var parsed = JsonSerializer.Deserialize<ParsedRecipe>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Title)) return null;

        var fullSourceDescription = $"{sourceDescription}: {parsed.Title}";
        var ingredients = new List<ImportedIngredientDto>();
        foreach (var ingredient in parsed.Ingredients)
        {
            var (canonicalName, category) = await _lookup.ResolveCanonical(ingredient.Name);
            await _lookup.QueueUnmatchedIfNeededAsync(ingredient.Name, category, requestedByUserId, fullSourceDescription);
            var unit = ingredient.Unit is null or "none" ? (UnitOfMeasure?)null : Enum.Parse<UnitOfMeasure>(ingredient.Unit, ignoreCase: true);
            ingredients.Add(new ImportedIngredientDto(canonicalName, Math.Max(1, ingredient.Quantity), ingredient.Amount, unit, category));
        }

        return new ImportedRecipeDto(parsed.Title, parsed.Description, ingredients);
    }

    private record ParsedRecipe(string Title, string Description, List<ParsedIngredient> Ingredients);
    private record ParsedIngredient(string Name, int Quantity, decimal? Amount, string? Unit);
    private record CleanedNamesResponse(List<CleanedNameEntry> Names);
    private record CleanedNameEntry(int Index, string Name);
}
