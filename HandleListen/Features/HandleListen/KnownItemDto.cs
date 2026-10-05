// Source tells the frontend which badge to show: "Known" = recognized by the central
// catalog (light blue checkmark), "History" = you've used this name before (light grey checkmark).
// DefaultUnit is only ever populated for "Known" matches, used to pre-fill the unit dropdown
// on a recipe ingredient (still changeable by the user).
public record CategorySuggestion(string? Category, string? Source, UnitOfMeasure? DefaultUnit = null);

public record KnownItemDto(int Id, string CanonicalName, string Category, UnitOfMeasure? DefaultUnit, List<string> Aliases);

public record CreateKnownItemRequest(string Name, string Category, UnitOfMeasure? DefaultUnit);

public record AddAliasRequest(string Alias);

public record MeDto(string Email, List<string> Roles);
