public record ShareRecipeRequest(int RecipeId);

public record SharedRecipeDto(int Id, string Title, string Description, string SharedByEmail, DateTime SharedAt, int AddedCount, List<string> Tags, bool SharedByMe);

public record SharedRecipeIngredientDto(string Name, int Quantity, decimal? Amount, UnitOfMeasure? Unit, string Category);

public record SharedRecipeDetailDto(int Id, string Title, string Description, string SharedByEmail, DateTime SharedAt, int AddedCount, List<string> Tags, List<SharedRecipeIngredientDto> Ingredients);

public record LogSearchRequest(string Query);

public record TopSearchDto(string Query, int Count);
