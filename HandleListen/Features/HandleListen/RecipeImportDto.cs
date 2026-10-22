public record ImportRecipeFromUrlRequest(string Url);

public record ImportRecipeFromTextRequest(string Text);

public record CleanIngredientNamesRequest(List<string> Names);

public record CleanedIngredientDto(string Name, string? Category);

public record ImportedIngredientDto(string Name, int Quantity, decimal? Amount, UnitOfMeasure? Unit, string? Category);

public record ImportedRecipeDto(string Title, string Description, List<ImportedIngredientDto> Ingredients);
