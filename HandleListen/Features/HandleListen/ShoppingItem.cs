using System.Text.Json.Serialization;

public class ShoppingItem
{
    public int ShoppingListId { get; set; }
    [JsonIgnore]
    public ShoppingList? shoppingList { get; set; }
    public int Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}