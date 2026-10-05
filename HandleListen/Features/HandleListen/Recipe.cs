public class Recipe
{
    public int Id { get; set; }
    public int RecipeBookId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
}
