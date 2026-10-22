using Microsoft.EntityFrameworkCore;

// Shared by CalendarController and RecipeBookController's CreateInvite - a single check rather
// than duplicating it, since both need exactly the same "has the recipient blocked the sender" test.
public class BlockService
{
    private readonly AppDbContext _context;

    public BlockService(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> HasBlockedAsync(string blockerUserId, string otherUserId) =>
        _context.BlockedUsers.AnyAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == otherUserId);
}
