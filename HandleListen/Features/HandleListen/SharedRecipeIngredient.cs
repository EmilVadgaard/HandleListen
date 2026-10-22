public class SharedRecipeIngredient
{
    public int Id { get; set; }
    public int SharedRecipeId { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; } = 1;
    public string Category { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public UnitOfMeasure? Unit { get; set; }
}
