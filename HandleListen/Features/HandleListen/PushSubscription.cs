public class PushSubscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public required string Token { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
