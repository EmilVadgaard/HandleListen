public record CalendarEventOccurrenceDto(
    int Id,
    string Title,
    string Description,
    DateOnly Date,
    TimeOnly? Time,
    int? ReminderValue,
    ReminderPeriod? ReminderUnit,
    RecurrenceFrequency? Recurrence,
    bool PushReminderEnabled,
    bool EmailReminderEnabled);
