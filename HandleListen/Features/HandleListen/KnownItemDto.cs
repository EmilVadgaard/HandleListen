// Source tells the frontend which badge to show: "Known" = recognized by the central
// catalog (light blue checkmark), "History" = you've used this name before (light grey checkmark).
public record CategorySuggestion(string? Category, string? Source);

public record KnownItemDto(int Id, string CanonicalName, string Category, List<string> Aliases);

public record CreateKnownItemRequest(string Name, string Category);

public record AddAliasRequest(string Alias);

public record MeDto(string Email, List<string> Roles);
