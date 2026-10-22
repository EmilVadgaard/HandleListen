// Guards against sending the same scheduled digest twice if the background sweep's trigger
// window (e.g. "it's Sunday, local time is past 07:00") is still open on its next poll.
public class DigestDelivery
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DigestFrequency Frequency { get; set; }
    public DateOnly SentDate { get; set; }
}
