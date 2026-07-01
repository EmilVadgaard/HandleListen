
public class ShoppingItem
{
    public int ShoppingListId { get; set; }
    public ShoppingList shoppingList { get; set; } = null!;
    public int Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}