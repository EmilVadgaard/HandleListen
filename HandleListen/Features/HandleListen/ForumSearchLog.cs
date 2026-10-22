// One row per distinct search submitted on the forum page - aggregated (grouped, counted,
// case-insensitively) to drive the "most searched" suggestion cards. Not tied to a user; the
// forum's search box is where this gets logged, nothing else.
public class ForumSearchLog
{
    public int Id { get; set; }
    public required string Query { get; set; }
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
