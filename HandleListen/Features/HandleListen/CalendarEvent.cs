public enum ReminderPeriod
{
    Hours,
    Days
}

public class CalendarEvent
{
    public int Id { get; set; }
    public int CalendarId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public int? ReminderValue { get; set; }
    public ReminderPeriod? ReminderUnit { get; set; }
}
