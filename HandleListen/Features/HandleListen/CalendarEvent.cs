public enum ReminderPeriod
{
    Hours,
    Days
}

public enum RecurrenceFrequency
{
    Weekly,
    Monthly,
    Yearly
}

public class CalendarEvent
{
    public int Id { get; set; }
    public int CalendarId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    // For a recurring event, this is the FIRST occurrence - later ones are computed from it
    // (see RecurringEventService), never stored as separate rows.
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public int? ReminderValue { get; set; }
    public ReminderPeriod? ReminderUnit { get; set; }
    public RecurrenceFrequency? Recurrence { get; set; }
    public bool PushReminderEnabled { get; set; } = true;
    public bool EmailReminderEnabled { get; set; } = false;
}
