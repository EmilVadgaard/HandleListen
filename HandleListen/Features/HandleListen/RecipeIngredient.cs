using System.Text.Json.Serialization;

public class RecipeIngredient
{
    public int RecipeId { get; set; }
    public int Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Category { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public UnitOfMeasure? Unit { get; set; }
    [JsonIgnore]
    public string? CreatedByUserId { get; set; }
}
