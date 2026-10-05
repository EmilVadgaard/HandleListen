public record RecipeDto(int Id, string Title, string Description, List<RecipeTagDto> Tags);

public record RecipeTagDto(int Id, string Tag);

public record UpdateRecipeRequest(string Title, string Description);

public record AddTagRequest(string Tag);
