// A frozen snapshot of a recipe shared to the public forum - deliberately NOT live-linked to the
// source Recipe, so a later edit (or deletion) of the original in the sharer's book never changes
// or breaks what other users already see/added from the forum.
public class SharedRecipe
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SharedByUserId { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;

    // How many times this forum post has been added into someone's own recipe book - the
    // "most used" sort key. Only ever increments; it's a usage counter, not a live count of
    // how many copies still exist (deleting a copy later doesn't decrement it).
    public int AddedCount { get; set; } = 0;

    // Informational only - lets a user's own recipe-card dialog show "already shared" for a
    // specific recipe. Not a foreign key: the original Recipe can be edited or deleted later
    // without touching this snapshot.
    public int? OriginalRecipeId { get; set; }
}
