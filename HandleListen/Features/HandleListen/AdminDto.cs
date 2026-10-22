public record KnownItemRequestDto(int Id, string Name, string? SourceDescription, string RequestedByEmail, DateTime CreatedAt, DateTime LastSeenAt, int SeenCount);
