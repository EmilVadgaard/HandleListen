using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class ShoppingListHub : Hub
{
    private readonly AppDbContext _context;
    private string UserId => Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public ShoppingListHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForUser(UserId));
        await base.OnConnectedAsync();
    }

    public async Task JoinList(int listId)
    {
        var accessible = await _context.ShoppingLists.AnyAsync(l => l.Id == listId &&
            (l.UserId == UserId || _context.ShoppingListGuests.Any(g => g.ShoppingListId == listId && g.UserId == UserId)));
        if (!accessible) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForList(listId));
    }

    public async Task LeaveList(int listId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForList(listId));
    }

    public async Task JoinCalendar(int calendarId)
    {
        var accessible = await _context.CalendarMembers.AnyAsync(m => m.CalendarId == calendarId && m.UserId == UserId);
        if (!accessible) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForCalendar(calendarId));
    }

    public async Task LeaveCalendar(int calendarId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForCalendar(calendarId));
    }

    public async Task JoinRecipe(int recipeId)
    {
        var accessible = await _context.Recipes.AnyAsync(r => r.Id == recipeId &&
            _context.RecipeBookMembers.Any(m => m.RecipeBookId == r.RecipeBookId && m.UserId == UserId));
        if (!accessible) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForRecipe(recipeId));
    }

    public async Task LeaveRecipe(int recipeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForRecipe(recipeId));
    }
}

public static class GroupNames
{
    public static string ForUser(string userId) => $"user-{userId}";
    public static string ForList(int listId) => $"list-{listId}";
    public static string ForCalendar(int calendarId) => $"calendar-{calendarId}";
    public static string ForRecipe(int recipeId) => $"recipe-{recipeId}";
}
