public record RecipeBookIdDto(int Id);

public record RecipeBookMemberDto(string UserId, string Email);

public record RecipeBookInviteDto(int Id, string FromEmail, string ToEmail, bool IsIncoming, DateTime CreatedAt);
