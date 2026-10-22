using Microsoft.EntityFrameworkCore;

// Separate from ReminderNotificationBackgroundService - unrelated cadence (once a day/week/month
// vs. every few minutes) and unrelated "due" logic (a calendar-wide day gate vs. a per-event
// reminder window).
public class DigestEmailBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeZoneInfo DanishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
    private static readonly TimeOnly SendTime = new(7, 0);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DigestEmailBackgroundService> _logger;

    public DigestEmailBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DigestEmailBackgroundService> logger)
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
                await ProcessDueDigestsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Digest email sweep failed");
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

    private static bool IsTriggerDay(DigestFrequency frequency, DateOnly today) => frequency switch
    {
        DigestFrequency.Daily => true,
        DigestFrequency.Weekly => today.DayOfWeek == DayOfWeek.Sunday,
        DigestFrequency.Monthly => today.AddDays(1).Month != today.Month,
        _ => false
    };

    private async Task ProcessDueDigestsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var builder = scope.ServiceProvider.GetRequiredService<DigestBuilderService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<CalendarEmailSender>();

        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DanishTimeZone);
        var today = DateOnly.FromDateTime(nowLocal);
        if (TimeOnly.FromDateTime(nowLocal) < SendTime) return;

        var preferences = await context.DigestPreferences.ToListAsync(ct);
        foreach (var preference in preferences)
        {
            if (!IsTriggerDay(preference.Frequency, today)) continue;

            var alreadySent = await context.DigestDeliveries
                .AnyAsync(d => d.UserId == preference.UserId && d.Frequency == preference.Frequency && d.SentDate == today, ct);
            if (alreadySent) continue;

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == preference.UserId, ct);
            if (user?.Email is null) continue;

            var (from, to) = DigestBuilderService.GetRange(preference.Frequency, today);
            var events = await builder.BuildEventListAsync(preference.UserId, from, to, ct);

            // No point mailing an empty list - skip quietly rather than sending "no events".
            if (events.Count > 0)
            {
                await emailSender.SendDigestEmailAsync(user.Email, SubjectFor(preference.Frequency), IntroFor(preference.Frequency), events);
            }

            context.DigestDeliveries.Add(new DigestDelivery { UserId = preference.UserId, Frequency = preference.Frequency, SentDate = today });
        }

        await context.SaveChangesAsync(ct);
    }

    public static string SubjectFor(DigestFrequency frequency) => frequency switch
    {
        DigestFrequency.Daily => "Dagens oversigt",
        DigestFrequency.Weekly => "Ugens oversigt",
        DigestFrequency.Monthly => "Månedens oversigt",
        _ => "Oversigt"
    };

    public static string IntroFor(DigestFrequency frequency) => frequency switch
    {
        DigestFrequency.Daily => "Kommende begivenheder de næste 7 dage:",
        DigestFrequency.Weekly => "Kommende begivenheder i den næste uge:",
        DigestFrequency.Monthly => "Kommende begivenheder i den næste måned:",
        _ => "Kommende begivenheder:"
    };
}
