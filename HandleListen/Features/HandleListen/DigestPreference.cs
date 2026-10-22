public enum DigestFrequency
{
    Daily,
    Weekly,
    Monthly
}

// A row only exists when the user has opted in - no row means "no digest emails", so there's
// no separate "None" enum member to keep in sync with "delete the row".
public class DigestPreference
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DigestFrequency Frequency { get; set; }
}
