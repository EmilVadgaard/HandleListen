using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

// Reads the schema.org/Recipe structured data that most serious recipe sites already embed in
// their page (the same data Google uses for recipe rich snippets) - no AI needed, and it fails
// cleanly (returns null) on pages that don't have it rather than guessing. Supports both of the
// two formats sites actually use in the wild: modern JSON-LD, and the older HTML
// itemprop/itemtype "Microdata" format still common on WordPress recipe plugins (e.g. several
// Danish food blogs use Microdata, not JSON-LD).
public class RecipeUrlImportService
{
    private static readonly (string Unit, UnitOfMeasure Value)[] UnitPatterns =
    {
        ("kg", UnitOfMeasure.Kilogram),
        ("g", UnitOfMeasure.Gram),
        ("liter", UnitOfMeasure.Liter),
        ("l", UnitOfMeasure.Liter),
        ("dl", UnitOfMeasure.Deciliter),
        ("ml", UnitOfMeasure.Milliliter),
        ("stk", UnitOfMeasure.Piece),
    };

    private readonly HttpClient _http;
    private readonly KnownItemLookupService _lookup;

    public RecipeUrlImportService(HttpClient http, KnownItemLookupService lookup)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(15);
        _lookup = lookup;
    }

    public async Task<ImportedRecipeDto?> ImportAsync(string url, string requestedByUserId)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            return null;

        string html;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; HandleListenBot/1.0)");
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            html = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var extracted = ExtractFromJsonLd(doc) ?? ExtractFromMicrodata(doc);
        if (extracted is null) return null;

        var ingredients = new List<ImportedIngredientDto>();
        foreach (var line in extracted.Value.IngredientLines)
        {
            ingredients.Add(await ParseIngredientLineAsync(line, requestedByUserId, $"Link-import: {extracted.Value.Title}"));
        }

        return new ImportedRecipeDto(extracted.Value.Title, extracted.Value.Description, ingredients);
    }

    private static (string Title, string Description, List<string> IngredientLines)? ExtractFromJsonLd(HtmlDocument doc)
    {
        var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scriptNodes is null) return null;

        foreach (var script in scriptNodes)
        {
            JsonDocument jsonDoc;
            try
            {
                jsonDoc = JsonDocument.Parse(HtmlEntity.DeEntitize(script.InnerText));
            }
            catch (JsonException)
            {
                continue;
            }

            var recipeNode = FindRecipeJsonNode(jsonDoc.RootElement);
            if (recipeNode is null) continue;

            var title = GetJsonString(recipeNode.Value, "name") ?? "";
            var ingredientLines = GetJsonStringArray(recipeNode.Value, "recipeIngredient");
            var description = ExtractJsonInstructions(recipeNode.Value);
            return (title, description, ingredientLines);
        }

        return null;
    }

    private static JsonElement? FindRecipeJsonNode(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindRecipeJsonNode(item);
                if (found is not null) return found;
            }
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object) return null;

        if (element.TryGetProperty("@type", out var typeProp))
        {
            var isRecipe = typeProp.ValueKind == JsonValueKind.String
                ? typeProp.GetString() == "Recipe"
                : typeProp.ValueKind == JsonValueKind.Array &&
                  typeProp.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String && t.GetString() == "Recipe");
            if (isRecipe) return element;
        }

        if (element.TryGetProperty("@graph", out var graph))
        {
            var found = FindRecipeJsonNode(graph);
            if (found is not null) return found;
        }

        return null;
    }

    private static string? GetJsonString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;

    private static List<string> GetJsonStringArray(JsonElement element, string property)
    {
        var result = new List<string>();
        if (!element.TryGetProperty(property, out var prop)) return result;

        if (prop.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in prop.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) result.Add(s.Trim());
                }
            }
        }
        else if (prop.ValueKind == JsonValueKind.String)
        {
            var s = prop.GetString();
            if (!string.IsNullOrWhiteSpace(s)) result.Add(s.Trim());
        }

        return result;
    }

    private static string ExtractJsonInstructions(JsonElement recipe)
    {
        if (!recipe.TryGetProperty("recipeInstructions", out var instructions)) return "";

        var steps = new List<string>();
        CollectJsonInstructionSteps(instructions, steps);
        return string.Join("\n", steps.Select((s, i) => $"{i + 1}. {s}"));
    }

    private static void CollectJsonInstructionSteps(JsonElement element, List<string> steps)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text)) steps.Add(text.Trim());
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) CollectJsonInstructionSteps(item, steps);
                break;
            case JsonValueKind.Object:
                // HowToStep has "text"; HowToSection groups nested steps under "itemListElement".
                if (element.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                {
                    var t = textProp.GetString();
                    if (!string.IsNullOrWhiteSpace(t)) steps.Add(t.Trim());
                }
                else if (element.TryGetProperty("itemListElement", out var nested))
                {
                    CollectJsonInstructionSteps(nested, steps);
                }
                break;
        }
    }

    private static (string Title, string Description, List<string> IngredientLines)? ExtractFromMicrodata(HtmlDocument doc)
    {
        var recipeNode = doc.DocumentNode.SelectSingleNode("//*[@itemtype and contains(@itemtype, 'schema.org/Recipe')]");
        if (recipeNode is null) return null;

        // Recipe titles are reliably rendered as a heading; prefer that over the first
        // itemprop="name" match, which can just as easily belong to a nested author/publisher
        // Person/Organization (e.g. a site's own brand name) that appears earlier in the DOM.
        var titleNode = recipeNode.SelectSingleNode(".//*[(self::h1 or self::h2 or self::h3) and @itemprop='name']")
            ?? recipeNode.SelectSingleNode(".//*[@itemprop='name']");
        var title = HtmlEntity.DeEntitize(titleNode?.InnerText ?? "").Trim();

        var ingredientLines = recipeNode.SelectNodes(".//*[@itemprop='recipeIngredient']")
            ?.Select(n => HtmlEntity.DeEntitize(n.InnerText).Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? new List<string>();

        var steps = new List<string>();
        var instructionsNode = recipeNode.SelectSingleNode(".//*[@itemprop='recipeInstructions']");
        if (instructionsNode is not null)
        {
            var paragraphs = instructionsNode.SelectNodes(".//p");
            if (paragraphs is not null && paragraphs.Count > 0)
            {
                steps = paragraphs
                    .Select(p => HtmlEntity.DeEntitize(p.InnerText).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
            else
            {
                var text = HtmlEntity.DeEntitize(instructionsNode.InnerText).Trim();
                if (!string.IsNullOrWhiteSpace(text)) steps.Add(text);
            }
        }
        var description = string.Join("\n", steps.Select((s, i) => $"{i + 1}. {s}"));

        return (title, description, ingredientLines);
    }

    private async Task<ImportedIngredientDto> ParseIngredientLineAsync(string line, string requestedByUserId, string sourceDescription)
    {
        var trimmed = Regex.Replace(line.Trim(), @"\s+", " ");

        var match = Regex.Match(trimmed, @"^(\d+(?:[.,]\d+)?)\s*([a-zA-ZæøåÆØÅ]*)\s*(.*)$");
        string name;
        int quantity = 1;
        decimal? amount = null;
        UnitOfMeasure? unit = null;

        if (match.Success && match.Groups[3].Value.Length > 0)
        {
            var numberStr = match.Groups[1].Value.Replace(",", ".");
            var unitWord = match.Groups[2].Value.ToLowerInvariant();
            var rest = match.Groups[3].Value.Trim();

            var unitMatch = UnitPatterns.FirstOrDefault(u => u.Unit == unitWord);
            if (unitMatch.Unit is not null && decimal.TryParse(numberStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount))
            {
                // e.g. "400 g hakket oksekød" -> amount 400 g, name "hakket oksekød", buy-quantity stays 1
                amount = parsedAmount;
                unit = unitMatch.Value;
                name = rest;
            }
            else if (unitWord.Length == 0 && int.TryParse(numberStr, out var parsedQuantity))
            {
                // e.g. "2 løg" -> no recognized unit, so the leading number is a purchase count
                quantity = parsedQuantity;
                name = rest;
            }
            else
            {
                // Recognized a number but not the following word as a unit (e.g. "2 store løg") -
                // keep that word as part of the name instead of silently dropping it.
                name = $"{unitWord} {rest}".Trim();
            }
        }
        else
        {
            name = trimmed;
        }

        var (canonicalName, category) = await _lookup.ResolveCanonical(name);
        await _lookup.QueueUnmatchedIfNeededAsync(name, category, requestedByUserId, sourceDescription);
        return new ImportedIngredientDto(canonicalName, quantity, amount, unit, category);
    }
}
