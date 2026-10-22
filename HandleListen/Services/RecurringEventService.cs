// Pure computation: expands a CalendarEvent's recurrence rule into concrete occurrence dates.
// Occurrences are never materialized as rows - every caller (calendar range queries, the
// reminder sweep, digest emails) calls this against the live rule + exception list instead.
public static class RecurringEventService
{
    public static IEnumerable<DateOnly> GetOccurrencesInRange(CalendarEvent calendarEvent, DateOnly from, DateOnly to, ISet<DateOnly> exceptions)
    {
        if (to < from) yield break;

        if (calendarEvent.Recurrence is null)
        {
            if (calendarEvent.Date >= from && calendarEvent.Date <= to && !exceptions.Contains(calendarEvent.Date))
                yield return calendarEvent.Date;
            yield break;
        }

        var anchor = calendarEvent.Date;
        var rangeStart = anchor > from ? anchor : from;
        if (rangeStart > to) yield break;

        switch (calendarEvent.Recurrence)
        {
            case RecurrenceFrequency.Weekly:
                // Jump straight to the first candidate on/after rangeStart instead of stepping
                // from the anchor one week at a time - the anchor can be years in the past.
                var daysSinceAnchor = rangeStart.DayNumber - anchor.DayNumber;
                var weeksToSkip = daysSinceAnchor / 7;
                var candidate = anchor.AddDays(weeksToSkip * 7);
                if (candidate < rangeStart) candidate = candidate.AddDays(7);
                for (; candidate <= to; candidate = candidate.AddDays(7))
                {
                    if (!exceptions.Contains(candidate)) yield return candidate;
                }
                break;

            case RecurrenceFrequency.Monthly:
                for (var month = new DateOnly(rangeStart.Year, rangeStart.Month, 1); month <= to; month = month.AddMonths(1))
                {
                    var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
                    var day = Math.Min(anchor.Day, daysInMonth);
                    var occurrence = new DateOnly(month.Year, month.Month, day);
                    if (occurrence >= rangeStart && occurrence <= to && !exceptions.Contains(occurrence))
                        yield return occurrence;
                }
                break;

            case RecurrenceFrequency.Yearly:
                for (var year = rangeStart.Year; year <= to.Year; year++)
                {
                    var daysInMonth = DateTime.DaysInMonth(year, anchor.Month);
                    var day = Math.Min(anchor.Day, daysInMonth);
                    var occurrence = new DateOnly(year, anchor.Month, day);
                    if (occurrence >= rangeStart && occurrence <= to && !exceptions.Contains(occurrence))
                        yield return occurrence;
                }
                break;
        }
    }

    // Used by the reminder sweep and digest builder: the single next occurrence on/after a
    // reference date. A one-year lookahead window is enough for all three recurrence types -
    // weekly/monthly occurrences are found within weeks, and yearly within at most a year.
    // Expands a whole batch of events against a pre-loaded exceptions-by-event-id map - the
    // shared shape used by both the calendar range query and the digest builder.
    public static IEnumerable<(CalendarEvent Event, DateOnly Date)> ExpandEvents(
        IEnumerable<CalendarEvent> events, DateOnly from, DateOnly to, IReadOnlyDictionary<int, ISet<DateOnly>> exceptionsByEvent)
    {
        foreach (var calendarEvent in events)
        {
            var exceptions = exceptionsByEvent.TryGetValue(calendarEvent.Id, out var set) ? set : new HashSet<DateOnly>();
            foreach (var date in GetOccurrencesInRange(calendarEvent, from, to, exceptions))
            {
                yield return (calendarEvent, date);
            }
        }
    }

    public static DateOnly? GetNextOccurrenceOnOrAfter(CalendarEvent calendarEvent, DateOnly onOrAfter, ISet<DateOnly> exceptions) =>
        GetOccurrencesInRange(calendarEvent, onOrAfter, onOrAfter.AddYears(1), exceptions)
            .Select(d => (DateOnly?)d)
            .FirstOrDefault();
}
