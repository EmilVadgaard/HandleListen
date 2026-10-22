using Microsoft.EntityFrameworkCore;

// Shared between the scheduled sweep (DigestEmailBackgroundService) and the on-demand
// "send now" endpoint, so both always build identical content for a given frequency.
public class DigestBuilderService
{
    private readonly AppDbContext _context;

    public DigestBuilderService(AppDbContext context)
    {
        _context = context;
    }

    // Always computed relative to `today`, regardless of which day it actually is - the
    // scheduled sweep only calls this on the right trigger day (see DigestEmailBackgroundService),
    // while "send now" calls it any day to preview "what the next scheduled send would contain".
    public static (DateOnly From, DateOnly To) GetRange(DigestFrequency frequency, DateOnly today) => frequency switch
    {
        DigestFrequency.Daily => (today, today.AddDays(6)),
        // Scheduled to go out Sunday mornings, about the week that starts the next day.
        DigestFrequency.Weekly => (today.AddDays(1), today.AddDays(7)),
        DigestFrequency.Monthly => GetNextMonthRange(today),
        _ => throw new ArgumentOutOfRangeException(nameof(frequency))
    };

    private static (DateOnly From, DateOnly To) GetNextMonthRange(DateOnly today)
    {
        var nextMonthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        var nextMonthEnd = nextMonthStart.AddMonths(1).AddDays(-1);
        return (nextMonthStart, nextMonthEnd);
    }

    public async Task<List<(string Title, DateOnly Date, TimeOnly? Time)>> BuildEventListAsync(string userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var calendarId = await _context.CalendarMembers.Where(m => m.UserId == userId).Select(m => m.CalendarId).FirstOrDefaultAsync(ct);
        if (calendarId == 0) return [];

        var events = await _context.CalendarEvents.Where(e => e.CalendarId == calendarId).ToListAsync(ct);
        var recurringIds = events.Where(e => e.Recurrence is not null).Select(e => e.Id).ToList();
        var exceptionsByEvent = (await _context.CalendarEventExceptions
                .Where(x => recurringIds.Contains(x.CalendarEventId))
                .ToListAsync(ct))
            .GroupBy(x => x.CalendarEventId)
            .ToDictionary(g => g.Key, g => (ISet<DateOnly>)g.Select(x => x.ExceptionDate).ToHashSet());

        return RecurringEventService.ExpandEvents(events, from, to, exceptionsByEvent)
            .Select(x => (x.Event.Title, x.Date, x.Event.Time))
            .ToList();
    }
}
