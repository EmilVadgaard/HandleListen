public record BlockedUserDto(int Id, string Email);

public record BlockUserRequest(string Email);

public record AcceptPrivacyPolicyRequest(string Email);
