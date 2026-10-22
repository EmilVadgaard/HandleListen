// Tracks push resend state per (event, occurrence, user) - OccurrenceDate matters now that one
// CalendarEvent row can represent many occurrences (recurring events): the delivery state for
// this week's instance must not be confused with next week's.
public class ReminderDelivery
{
    public int Id { get; set; }
    public int CalendarEventId { get; set; }
    public DateOnly OccurrenceDate { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime LastSentAt { get; set; }
}
