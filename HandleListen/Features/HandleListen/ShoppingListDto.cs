public record ShoppingListDto(int Id, string Name, bool IsOwner, string OwnerEmail);

public record GuestDto(string Id, string Email);

public record ShareRequest(string Email);

public record UpdateListRequest(string Name);

public record MergeListsRequest(string? Name, List<int> ListIds);
