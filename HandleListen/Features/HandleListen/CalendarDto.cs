public record CalendarIdDto(int Id);

public record CalendarMemberDto(string UserId, string Email);

public record CalendarInviteDto(int Id, string FromEmail, string ToEmail, bool IsIncoming, DateTime CreatedAt);

public record InviteRequest(string Email);
