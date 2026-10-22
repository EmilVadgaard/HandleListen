using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _context;

    public AccountController(UserManager<IdentityUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return new MeDto(user.Email ?? string.Empty, roles.ToList());
    }

    // Called right after registration, before the user has a token - the account already exists
    // (created by the preceding /register call) but there's no session yet to authorize against.
    [AllowAnonymous]
    [HttpPost("accept-privacy-policy")]
    public async Task<IActionResult> AcceptPrivacyPolicy(AcceptPrivacyPolicyRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (user is null) return NotFound();

        _context.PrivacyPolicyAcceptances.Add(new PrivacyPolicyAcceptance { UserId = user.Id });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("blocked-users")]
    public async Task<ActionResult<IEnumerable<BlockedUserDto>>> GetBlockedUsers()
    {
        var userId = _userManager.GetUserId(User)!;
        var blocks = await _context.BlockedUsers.Where(b => b.BlockerUserId == userId).ToListAsync();
        var blockedIds = blocks.Select(b => b.BlockedUserId).ToList();
        var users = await _context.Users.Where(u => blockedIds.Contains(u.Id)).ToListAsync();

        return blocks
            .Select(b => new BlockedUserDto(b.Id, users.FirstOrDefault(u => u.Id == b.BlockedUserId)?.Email ?? ""))
            .ToList();
    }

    [Authorize]
    [HttpPost("blocked-users")]
    public async Task<IActionResult> BlockUser(BlockUserRequest request)
    {
        var userId = _userManager.GetUserId(User)!;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var target = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (target is null) return NotFound("Ingen bruger fundet med den email.");
        if (target.Id == userId) return BadRequest("Du kan ikke blokere dig selv.");

        var exists = await _context.BlockedUsers.AnyAsync(b => b.BlockerUserId == userId && b.BlockedUserId == target.Id);
        if (!exists)
        {
            _context.BlockedUsers.Add(new BlockedUser { BlockerUserId = userId, BlockedUserId = target.Id });
            await _context.SaveChangesAsync();
        }
        return NoContent();
    }

    [Authorize]
    [HttpDelete("blocked-users/{id}")]
    public async Task<IActionResult> Unblock(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var block = await _context.BlockedUsers.FirstOrDefaultAsync(b => b.Id == id && b.BlockerUserId == userId);
        if (block is null) return NotFound();

        _context.BlockedUsers.Remove(block);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Erases this user's account and everything personally theirs. Content shared with others
    // (a calendar/recipe book they're not the last member of) is left intact for the remaining
    // members - only their own membership/personal rows are removed. If they were the last
    // member of a calendar or recipe book, that calendar/book (and everything in it) is deleted
    // too, since nobody would be able to reach it anymore anyway.
    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();
        var userId = user.Id;

        var myLists = await _context.ShoppingLists.Where(l => l.UserId == userId).ToListAsync();
        var myListIds = myLists.Select(l => l.Id).ToList();
        _context.ShoppingItems.RemoveRange(_context.ShoppingItems.Where(i => myListIds.Contains(i.ShoppingListId)));
        _context.ShoppingListGuests.RemoveRange(_context.ShoppingListGuests.Where(g => myListIds.Contains(g.ShoppingListId)));
        _context.ShoppingLists.RemoveRange(myLists);
        _context.ShoppingListGuests.RemoveRange(_context.ShoppingListGuests.Where(g => g.UserId == userId));

        var myCalendarMemberships = await _context.CalendarMembers.Where(m => m.UserId == userId).ToListAsync();
        foreach (var membership in myCalendarMemberships)
        {
            _context.CalendarMembers.Remove(membership);
            var remaining = await _context.CalendarMembers.CountAsync(m => m.CalendarId == membership.CalendarId && m.UserId != userId);
            if (remaining == 0)
            {
                var eventIds = await _context.CalendarEvents.Where(e => e.CalendarId == membership.CalendarId).Select(e => e.Id).ToListAsync();
                _context.CalendarEventExceptions.RemoveRange(_context.CalendarEventExceptions.Where(x => eventIds.Contains(x.CalendarEventId)));
                _context.ReminderDeliveries.RemoveRange(_context.ReminderDeliveries.Where(x => eventIds.Contains(x.CalendarEventId)));
                _context.EmailReminderDeliveries.RemoveRange(_context.EmailReminderDeliveries.Where(x => eventIds.Contains(x.CalendarEventId)));
                _context.CalendarEvents.RemoveRange(_context.CalendarEvents.Where(e => e.CalendarId == membership.CalendarId));
                var calendar = await _context.Calendars.FindAsync(membership.CalendarId);
                if (calendar is not null) _context.Calendars.Remove(calendar);
            }
        }
        _context.CalendarInvites.RemoveRange(_context.CalendarInvites.Where(i => i.FromUserId == userId || i.ToUserId == userId));

        var myBookMemberships = await _context.RecipeBookMembers.Where(m => m.UserId == userId).ToListAsync();
        foreach (var membership in myBookMemberships)
        {
            _context.RecipeBookMembers.Remove(membership);
            var remaining = await _context.RecipeBookMembers.CountAsync(m => m.RecipeBookId == membership.RecipeBookId && m.UserId != userId);
            if (remaining == 0)
            {
                var recipeIds = await _context.Recipes.Where(r => r.RecipeBookId == membership.RecipeBookId).Select(r => r.Id).ToListAsync();
                _context.RecipeTags.RemoveRange(_context.RecipeTags.Where(t => recipeIds.Contains(t.RecipeId)));
                _context.RecipeIngredients.RemoveRange(_context.RecipeIngredients.Where(i => recipeIds.Contains(i.RecipeId)));
                var mealPlanIds = await _context.MealPlans.Where(p => p.RecipeBookId == membership.RecipeBookId).Select(p => p.Id).ToListAsync();
                _context.MealPlanRecipes.RemoveRange(_context.MealPlanRecipes.Where(mpr => mealPlanIds.Contains(mpr.MealPlanId)));
                _context.MealPlans.RemoveRange(_context.MealPlans.Where(p => p.RecipeBookId == membership.RecipeBookId));
                _context.Recipes.RemoveRange(_context.Recipes.Where(r => r.RecipeBookId == membership.RecipeBookId));
                var book = await _context.RecipeBooks.FindAsync(membership.RecipeBookId);
                if (book is not null) _context.RecipeBooks.Remove(book);
            }
        }
        _context.RecipeBookInvites.RemoveRange(_context.RecipeBookInvites.Where(i => i.FromUserId == userId || i.ToUserId == userId));

        var myBudgetMemberships = await _context.BudgetMembers.Where(m => m.UserId == userId).ToListAsync();
        foreach (var membership in myBudgetMemberships)
        {
            _context.BudgetMembers.Remove(membership);
            var remaining = await _context.BudgetMembers.CountAsync(m => m.BudgetId == membership.BudgetId && m.UserId != userId);
            if (remaining == 0)
            {
                _context.BudgetFixedEntries.RemoveRange(_context.BudgetFixedEntries.Where(e => e.BudgetId == membership.BudgetId));
                _context.BudgetVariableCategories.RemoveRange(_context.BudgetVariableCategories.Where(c => c.BudgetId == membership.BudgetId));
                var budget = await _context.Budgets.FindAsync(membership.BudgetId);
                if (budget is not null) _context.Budgets.Remove(budget);
            }
        }
        _context.BudgetInvites.RemoveRange(_context.BudgetInvites.Where(i => i.FromUserId == userId || i.ToUserId == userId));

        _context.PushSubscriptions.RemoveRange(_context.PushSubscriptions.Where(s => s.UserId == userId));
        _context.DigestPreferences.RemoveRange(_context.DigestPreferences.Where(p => p.UserId == userId));
        _context.DigestDeliveries.RemoveRange(_context.DigestDeliveries.Where(d => d.UserId == userId));
        _context.ReminderDeliveries.RemoveRange(_context.ReminderDeliveries.Where(d => d.UserId == userId));
        _context.EmailReminderDeliveries.RemoveRange(_context.EmailReminderDeliveries.Where(d => d.UserId == userId));
        _context.BlockedUsers.RemoveRange(_context.BlockedUsers.Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId));
        _context.KnownItemRequests.RemoveRange(_context.KnownItemRequests.Where(r => r.RequestedByUserId == userId));
        _context.PrivacyPolicyAcceptances.RemoveRange(_context.PrivacyPolicyAcceptances.Where(p => p.UserId == userId));

        var myPlans = await _context.Plans.Where(p => p.UserId == userId).ToListAsync();
        var myPlanIds = myPlans.Select(p => p.Id).ToList();
        _context.PlanTodos.RemoveRange(_context.PlanTodos.Where(t => myPlanIds.Contains(t.PlanId)));
        _context.PlanGuests.RemoveRange(_context.PlanGuests.Where(g => myPlanIds.Contains(g.PlanId)));
        _context.Plans.RemoveRange(myPlans);
        _context.PlanGuests.RemoveRange(_context.PlanGuests.Where(g => g.UserId == userId));
        _context.PersonalNotes.RemoveRange(_context.PersonalNotes.Where(n => n.UserId == userId));

        await _context.SaveChangesAsync();

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded) return BadRequest("Kunne ikke slette kontoen.");

        return NoContent();
    }
}
