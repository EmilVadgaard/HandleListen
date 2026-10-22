// One-shot - unlike ReminderDelivery (push), an email reminder is sent exactly once per
// (event, occurrence, user) when the reminder window opens, never resent as the event approaches.
public class EmailReminderDelivery
{
    public int Id { get; set; }
    public int CalendarEventId { get; set; }
    public DateOnly OccurrenceDate { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
