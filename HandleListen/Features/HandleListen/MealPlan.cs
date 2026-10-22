public class MealPlan
{
    public int Id { get; set; }
    public int RecipeBookId { get; set; }
    public required string Name { get; set; }
    public bool IsCurrent { get; set; } = false;
}
