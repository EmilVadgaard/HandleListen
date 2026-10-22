using Microsoft.EntityFrameworkCore;

public class ReminderNotificationBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeZoneInfo DanishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderNotificationBackgroundService> _logger;

    public ReminderNotificationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderNotificationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reminder notification sweep failed");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private static TimeSpan IntervalFor(TimeSpan timeUntilStart)
    {
        if (timeUntilStart > TimeSpan.FromHours(1)) return TimeSpan.FromHours(1);
        if (timeUntilStart > TimeSpan.FromMinutes(15)) return TimeSpan.FromMinutes(15);
        return TimeSpan.FromMinutes(5);
    }

    private async Task ProcessDueRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pushSender = scope.ServiceProvider.GetRequiredService<FcmPushSender>();
        var emailSender = scope.ServiceProvider.GetRequiredService<CalendarEmailSender>();

        // Calendar Date/Time are entered and shown in Danish local time, not UTC, so "now" has
        // to be converted into the same zone before comparing - otherwise reminders fire hours
        // off depending on the server's own timezone and daylight saving.
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DanishTimeZone);
        var today = DateOnly.FromDateTime(nowLocal);

        var candidates = await context.CalendarEvents
            .Where(e => e.ReminderValue != null && (e.PushReminderEnabled || e.EmailReminderEnabled))
            .ToListAsync(ct);

        var recurringIds = candidates.Where(e => e.Recurrence is not null).Select(e => e.Id).ToList();
        var exceptionsByEvent = (await context.CalendarEventExceptions
                .Where(x => recurringIds.Contains(x.CalendarEventId))
                .ToListAsync(ct))
            .GroupBy(x => x.CalendarEventId)
            .ToDictionary(g => g.Key, g => (ISet<DateOnly>)g.Select(x => x.ExceptionDate).ToHashSet());

        var dueEvents = new List<(CalendarEvent Event, DateOnly OccurrenceDate, DateTime EventDateTime, TimeSpan Interval)>();
        foreach (var calendarEvent in candidates)
        {
            // For a recurring event, calendarEvent.Date is only ever the FIRST occurrence - the
            // sweep re-derives "which occurrence is next" every run, so a weekly/monthly/yearly
            // reminder keeps firing for every future occurrence, not just the original one.
            var exceptions = exceptionsByEvent.TryGetValue(calendarEvent.Id, out var set) ? set : new HashSet<DateOnly>();
            var occurrenceDate = calendarEvent.Recurrence is null
                ? calendarEvent.Date
                : RecurringEventService.GetNextOccurrenceOnOrAfter(calendarEvent, today, exceptions);
            if (occurrenceDate is null) continue;

            var eventDateTime = occurrenceDate.Value.ToDateTime(calendarEvent.Time ?? TimeOnly.MinValue);
            if (eventDateTime < nowLocal) continue;

            var reminderSpan = calendarEvent.ReminderUnit == ReminderPeriod.Days
                ? TimeSpan.FromDays(calendarEvent.ReminderValue!.Value)
                : TimeSpan.FromHours(calendarEvent.ReminderValue!.Value);
            var reminderStart = eventDateTime - reminderSpan;
            if (nowLocal < reminderStart) continue;

            dueEvents.Add((calendarEvent, occurrenceDate.Value, eventDateTime, IntervalFor(eventDateTime - nowLocal)));
        }

        if (dueEvents.Count == 0) return;

        var calendarIds = dueEvents.Select(d => d.Event.CalendarId).Distinct().ToList();
        var members = await context.CalendarMembers
            .Where(m => calendarIds.Contains(m.CalendarId))
            .ToListAsync(ct);
        var memberUserIds = members.Select(m => m.UserId).Distinct().ToList();
        var users = await context.Users.Where(u => memberUserIds.Contains(u.Id)).ToListAsync(ct);

        foreach (var (calendarEvent, occurrenceDate, eventDateTime, interval) in dueEvents)
        {
            var calendarMemberUserIds = members.Where(m => m.CalendarId == calendarEvent.CalendarId).Select(m => m.UserId).Distinct();
            foreach (var userId in calendarMemberUserIds)
            {
                if (calendarEvent.PushReminderEnabled)
                {
                    await ProcessPushReminderAsync(context, pushSender, calendarEvent, occurrenceDate, eventDateTime, interval, userId, ct);
                }

                if (calendarEvent.EmailReminderEnabled)
                {
                    var email = users.FirstOrDefault(u => u.Id == userId)?.Email;
                    if (!string.IsNullOrEmpty(email))
                    {
                        await ProcessEmailReminderAsync(context, emailSender, calendarEvent, occurrenceDate, userId, email, ct);
                    }
                }
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private static async Task ProcessPushReminderAsync(AppDbContext context, FcmPushSender sender, CalendarEvent calendarEvent, DateOnly occurrenceDate, DateTime eventDateTime, TimeSpan interval, string userId, CancellationToken ct)
    {
        var delivery = await context.ReminderDeliveries
            .FirstOrDefaultAsync(d => d.CalendarEventId == calendarEvent.Id && d.OccurrenceDate == occurrenceDate && d.UserId == userId, ct);
        var dueForResend = delivery is null || (DateTime.UtcNow - delivery.LastSentAt) >= interval;
        if (!dueForResend) return;

        var subscriptions = await context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync(ct);
        // If the user has no registered device yet, don't mark this as delivered - that would
        // wrongly start the resend interval even though nothing was ever sent, delaying the
        // first real attempt by up to an hour once a device does register.
        if (subscriptions.Count == 0) return;

        var isFirstAlert = delivery is null;
        var body = $"{occurrenceDate:dd/MM} kl. {(calendarEvent.Time ?? TimeOnly.MinValue):HH\\:mm}";
        foreach (var subscription in subscriptions)
        {
            var keep = await sender.SendAsync(subscription.Token, calendarEvent.Title, body, $"calendar-event-{calendarEvent.Id}-{occurrenceDate:yyyyMMdd}", isFirstAlert);
            if (!keep) context.PushSubscriptions.Remove(subscription);
        }

        if (delivery is null)
        {
            context.ReminderDeliveries.Add(new ReminderDelivery { CalendarEventId = calendarEvent.Id, OccurrenceDate = occurrenceDate, UserId = userId, LastSentAt = DateTime.UtcNow });
        }
        else
        {
            delivery.LastSentAt = DateTime.UtcNow;
        }
    }

    private static async Task ProcessEmailReminderAsync(AppDbContext context, CalendarEmailSender sender, CalendarEvent calendarEvent, DateOnly occurrenceDate, string userId, string email, CancellationToken ct)
    {
        // One-shot - unlike push, this never resends as the event approaches.
        var alreadySent = await context.EmailReminderDeliveries
            .AnyAsync(d => d.CalendarEventId == calendarEvent.Id && d.OccurrenceDate == occurrenceDate && d.UserId == userId, ct);
        if (alreadySent) return;

        await sender.SendReminderEmailAsync(email, calendarEvent.Title, occurrenceDate, calendarEvent.Time);
        context.EmailReminderDeliveries.Add(new EmailReminderDelivery { CalendarEventId = calendarEvent.Id, OccurrenceDate = occurrenceDate, UserId = userId, SentAt = DateTime.UtcNow });
    }
}
