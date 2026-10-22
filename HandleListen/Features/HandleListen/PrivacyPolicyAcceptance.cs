// Recorded right after registration (see AccountController.AcceptPrivacyPolicy) - a separate
// table rather than a column on IdentityUser, to avoid subclassing/extending Identity's own type.
public class PrivacyPolicyAcceptance
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
}
