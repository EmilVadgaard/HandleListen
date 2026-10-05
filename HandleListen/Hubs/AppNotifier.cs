using Microsoft.AspNetCore.SignalR;

public class AppNotifier
{
    private readonly IHubContext<ShoppingListHub> _hub;

    public AppNotifier(IHubContext<ShoppingListHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyItemsChanged(int listId) =>
        _hub.Clients.Group(GroupNames.ForList(listId)).SendAsync("ItemsChanged", listId);

    public Task NotifyListsChanged(IEnumerable<string> userIds) =>
        Task.WhenAll(userIds.Distinct().Select(userId =>
            _hub.Clients.Group(GroupNames.ForUser(userId)).SendAsync("ListsChanged")));

    public Task NotifyCalendarEventsChanged(int calendarId) =>
        _hub.Clients.Group(GroupNames.ForCalendar(calendarId)).SendAsync("CalendarEventsChanged", calendarId);

    public Task NotifyCalendarChanged(IEnumerable<string> userIds) =>
        Task.WhenAll(userIds.Distinct().Select(userId =>
            _hub.Clients.Group(GroupNames.ForUser(userId)).SendAsync("CalendarChanged")));

    public Task NotifyRecipeIngredientsChanged(int recipeId) =>
        _hub.Clients.Group(GroupNames.ForRecipe(recipeId)).SendAsync("RecipeIngredientsChanged", recipeId);

    public Task NotifyRecipesChanged(IEnumerable<string> userIds) =>
        Task.WhenAll(userIds.Distinct().Select(userId =>
            _hub.Clients.Group(GroupNames.ForUser(userId)).SendAsync("RecipesChanged")));
}
