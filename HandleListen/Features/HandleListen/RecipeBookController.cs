using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/recipe-book")]
public class RecipeBookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RecipeBookAccessService _bookAccess;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public RecipeBookController(AppDbContext context, RecipeBookAccessService bookAccess, AppNotifier notifier)
    {
        _context = context;
        _bookAccess = bookAccess;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<ActionResult<RecipeBookIdDto>> GetMine()
    {
        var bookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);
        return new RecipeBookIdDto(bookId);
    }

    [HttpPost("leave")]
    public async Task<IActionResult> Leave()
    {
        var bookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);

        var otherMemberIds = await _context.RecipeBookMembers
            .Where(m => m.RecipeBookId == bookId && m.UserId != UserId)
            .Select(m => m.UserId)
            .ToListAsync();
        if (otherMemberIds.Count == 0) return BadRequest("Du deler ikke din opskriftsbog med nogen.");

        var myMember = await _context.RecipeBookMembers
            .FirstAsync(m => m.RecipeBookId == bookId && m.UserId == UserId);

        var newBook = new RecipeBook();
        _context.RecipeBooks.Add(newBook);
        await _context.SaveChangesAsync();

        var existingRecipes = await _context.Recipes
            .Where(r => r.RecipeBookId == bookId)
            .ToListAsync();
        var existingRecipeIds = existingRecipes.Select(r => r.Id).ToList();
        var existingIngredients = await _context.RecipeIngredients
            .Where(i => existingRecipeIds.Contains(i.RecipeId))
            .ToListAsync();
        var existingTags = await _context.RecipeTags
            .Where(t => existingRecipeIds.Contains(t.RecipeId))
            .ToListAsync();

        foreach (var recipe in existingRecipes)
        {
            var newRecipe = new Recipe { RecipeBookId = newBook.Id, Title = recipe.Title, Description = recipe.Description };
            _context.Recipes.Add(newRecipe);
            await _context.SaveChangesAsync();

            foreach (var ingredient in existingIngredients.Where(i => i.RecipeId == recipe.Id))
            {
                _context.RecipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = newRecipe.Id,
                    Name = ingredient.Name,
                    Quantity = ingredient.Quantity,
                    CreatedByUserId = ingredient.CreatedByUserId
                });
            }

            foreach (var tag in existingTags.Where(t => t.RecipeId == recipe.Id))
            {
                _context.RecipeTags.Add(new RecipeTag { RecipeId = newRecipe.Id, Tag = tag.Tag });
            }
        }
        await _context.SaveChangesAsync();

        myMember.RecipeBookId = newBook.Id;
        await _context.SaveChangesAsync();

        await _notifier.NotifyRecipesChanged(otherMemberIds.Append(UserId));
        return NoContent();
    }

    [HttpGet("members")]
    public async Task<ActionResult<IEnumerable<RecipeBookMemberDto>>> GetMembers()
    {
        var bookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);
        return await _context.RecipeBookMembers
            .Where(m => m.RecipeBookId == bookId)
            .Join(_context.Users, m => m.UserId, u => u.Id, (m, u) => new RecipeBookMemberDto(u.Id, u.Email!))
            .ToListAsync();
    }

    [HttpGet("invites")]
    public async Task<ActionResult<IEnumerable<RecipeBookInviteDto>>> GetInvites()
    {
        var invites = await _context.RecipeBookInvites
            .Where(i => i.FromUserId == UserId || i.ToUserId == UserId)
            .ToListAsync();

        var userIds = invites.SelectMany(i => new[] { i.FromUserId, i.ToUserId }).Distinct().ToList();
        var emails = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email!);

        return invites
            .Select(i => new RecipeBookInviteDto(
                i.Id,
                emails.GetValueOrDefault(i.FromUserId, ""),
                emails.GetValueOrDefault(i.ToUserId, ""),
                i.ToUserId == UserId,
                i.CreatedAt))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }

    [HttpPost("invites")]
    public async Task<ActionResult<RecipeBookInviteDto>> CreateInvite(InviteRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var toUser = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (toUser is null) return NotFound("Ingen bruger fundet med den email.");
        if (toUser.Id == UserId) return BadRequest("Du kan ikke invitere dig selv.");

        var myBookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);
        var theirBookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(toUser.Id);
        if (myBookId == theirBookId) return Conflict("I deler allerede opskriftsbog.");

        var alreadyInvited = await _context.RecipeBookInvites.AnyAsync(i =>
            (i.FromUserId == UserId && i.ToUserId == toUser.Id) ||
            (i.FromUserId == toUser.Id && i.ToUserId == UserId));
        if (alreadyInvited) return Conflict("Der findes allerede en invitation mellem jer.");

        var invite = new RecipeBookInvite { FromUserId = UserId, ToUserId = toUser.Id };
        _context.RecipeBookInvites.Add(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyRecipesChanged([toUser.Id]);

        var myEmail = await _context.Users.Where(u => u.Id == UserId).Select(u => u.Email!).FirstAsync();
        return new RecipeBookInviteDto(invite.Id, myEmail, toUser.Email!, false, invite.CreatedAt);
    }

    [HttpPost("invites/{id}/accept")]
    public async Task<IActionResult> AcceptInvite(int id)
    {
        var invite = await _context.RecipeBookInvites.FirstOrDefaultAsync(i => i.Id == id && i.ToUserId == UserId);
        if (invite is null) return NotFound();

        var fromBookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(invite.FromUserId);
        var toBookId = await _bookAccess.GetOrCreateRecipeBookIdAsync(UserId);

        var affectedUserIds = new List<string>();

        if (fromBookId != toBookId)
        {
            var members = await _context.RecipeBookMembers
                .Where(m => m.RecipeBookId == fromBookId || m.RecipeBookId == toBookId)
                .ToListAsync();
            affectedUserIds.AddRange(members.Select(m => m.UserId));

            var mergedBook = new RecipeBook();
            _context.RecipeBooks.Add(mergedBook);
            await _context.SaveChangesAsync();

            var recipes = await _context.Recipes
                .Where(r => r.RecipeBookId == fromBookId || r.RecipeBookId == toBookId)
                .ToListAsync();
            foreach (var recipe in recipes) recipe.RecipeBookId = mergedBook.Id;

            foreach (var member in members) member.RecipeBookId = mergedBook.Id;

            await _context.SaveChangesAsync();

            var oldBooks = await _context.RecipeBooks
                .Where(b => b.Id == fromBookId || b.Id == toBookId)
                .ToListAsync();
            _context.RecipeBooks.RemoveRange(oldBooks);
        }

        _context.RecipeBookInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyRecipesChanged(affectedUserIds);
        return NoContent();
    }

    [HttpPost("invites/{id}/decline")]
    public async Task<IActionResult> DeclineInvite(int id)
    {
        var invite = await _context.RecipeBookInvites.FirstOrDefaultAsync(i => i.Id == id && i.ToUserId == UserId);
        if (invite is null) return NotFound();

        _context.RecipeBookInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyRecipesChanged([invite.FromUserId]);
        return NoContent();
    }

    [HttpDelete("invites/{id}")]
    public async Task<IActionResult> CancelInvite(int id)
    {
        var invite = await _context.RecipeBookInvites.FirstOrDefaultAsync(i => i.Id == id && i.FromUserId == UserId);
        if (invite is null) return NotFound();

        _context.RecipeBookInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyRecipesChanged([invite.ToUserId]);
        return NoContent();
    }
}
