
public class ShoppingList
{
    public string UserId { get; set; } = string.Empty;
    public string[] GuestIds { get; set; } = Array.Empty<string>();
    public int Id { get; set; }
    public List<ShoppingItem> shoppingItems { get; set; } = new();
    public required string Name { get; set; } = string.Empty;
}