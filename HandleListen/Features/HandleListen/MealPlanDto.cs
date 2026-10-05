public record RecipeSummaryDto(int Id, string Title);

public record MealPlanDto(int Id, string Name, List<RecipeSummaryDto> Recipes);

public record CreateMealPlanRequest(string Name, List<int> RecipeIds);

public record GeneratedListItemDto(string Name, string Category, int Quantity, string? AmountSummary);

public record GenerateListItemRequest(string Name, string Category, int Quantity);

public record GenerateListRequest(string ListName, List<GenerateListItemRequest> Items);
