using Microsoft.EntityFrameworkCore;

public class RecipeBookAccessService
{
    private readonly AppDbContext _context;

    public RecipeBookAccessService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetOrCreateRecipeBookIdAsync(string userId)
    {
        var existingBookId = await _context.RecipeBookMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.RecipeBookId)
            .FirstOrDefaultAsync();
        if (existingBookId != 0) return existingBookId;

        var book = new RecipeBook();
        _context.RecipeBooks.Add(book);
        await _context.SaveChangesAsync();

        _context.RecipeBookMembers.Add(new RecipeBookMember { RecipeBookId = book.Id, UserId = userId });
        await _context.SaveChangesAsync();

        return book.Id;
    }
}
