// BlockerUserId blocked BlockedUserId - one-directional. The only effect: BlockedUserId can no
// longer send BlockerUserId a calendar/recipe-book invite (see BlockService).
public class BlockedUser
{
    public int Id { get; set; }
    public string BlockerUserId { get; set; } = string.Empty;
    public string BlockedUserId { get; set; } = string.Empty;
}
