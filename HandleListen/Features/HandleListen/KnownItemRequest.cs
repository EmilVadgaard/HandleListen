// A queued "this ingredient didn't match anything in the known-item catalog" notice, raised
// automatically during recipe import (link/AI-text/AI-image, and the AI ingredient-cleanup
// step) - never from ordinary manual typing elsewhere, which would flood this with noise.
// Owner reviews these later and decides whether to add the item/alias to the catalog.
public class KnownItemRequest
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? SourceDescription { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public int SeenCount { get; set; } = 1;
    public bool Resolved { get; set; } = false;
}
