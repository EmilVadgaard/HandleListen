using System.Net.Http.Headers;
using System.Net.Http.Json;

// Sibling to ResendEmailSender rather than an extension of it - that class's public surface is
// pinned to ASP.NET Identity's IEmailSender<IdentityUser> interface (confirm-email/reset-password
// only), unrelated to calendar reminders/digests. Same Resend REST transport underneath.
public class CalendarEmailSender
{
    private readonly HttpClient _http;
    private readonly ILogger<CalendarEmailSender> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;

    public CalendarEmailSender(HttpClient http, IConfiguration configuration, ILogger<CalendarEmailSender> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = configuration["Resend:ApiKey"] ?? string.Empty;
        _fromEmail = configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";
    }

    private static string FormatWhen(DateOnly date, TimeOnly? time) =>
        time is null ? $"{date:dd/MM/yyyy} (hele dagen)" : $"{date:dd/MM/yyyy} kl. {time:HH\\:mm}";

    public Task SendReminderEmailAsync(string toEmail, string eventTitle, DateOnly date, TimeOnly? time) =>
        SendAsync(toEmail, $"Påmindelse: {eventTitle}", $"<p><strong>{eventTitle}</strong></p><p>{FormatWhen(date, time)}</p>");

    public Task SendDigestEmailAsync(string toEmail, string subject, string introText, List<(string Title, DateOnly Date, TimeOnly? Time)> events)
    {
        var rows = string.Join("", events
            .OrderBy(e => e.Date).ThenBy(e => e.Time)
            .Select(e => $"<li>{FormatWhen(e.Date, e.Time)} - {e.Title}</li>"));
        var html = $"<p>{introText}</p><ul>{rows}</ul>";
        return SendAsync(toEmail, subject, html);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Resend:ApiKey is not configured. Email to {Email} was not sent. Subject: {Subject}\n{Body}", toEmail, subject, htmlBody);
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new
            {
                from = _fromEmail,
                to = new[] { toEmail },
                subject,
                html = htmlBody
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Resend returned {StatusCode} sending to {Email}: {Body}", response.StatusCode, toEmail, body);
        }
    }
}
