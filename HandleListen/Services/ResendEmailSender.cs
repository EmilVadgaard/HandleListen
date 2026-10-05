using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using Microsoft.AspNetCore.Identity;

public class ResendEmailSender : IEmailSender<IdentityUser>
{
    private readonly HttpClient _http;
    private readonly ILogger<ResendEmailSender> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _appBaseUrl;

    public ResendEmailSender(HttpClient http, IConfiguration configuration, ILogger<ResendEmailSender> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = configuration["Resend:ApiKey"] ?? string.Empty;
        _fromEmail = configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";
        _appBaseUrl = (configuration["App:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
    }

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink)
    {
        // confirmationLink points at the backend's own confirmEmail endpoint and is HTML-encoded;
        // pull the userId/code out of it and send the user to our own frontend page instead.
        var decoded = HttpUtility.HtmlDecode(confirmationLink);
        var query = HttpUtility.ParseQueryString(new Uri(decoded).Query);
        var link = $"{_appBaseUrl}/confirm-email?userId={Uri.EscapeDataString(query["userId"] ?? "")}&code={Uri.EscapeDataString(query["code"] ?? "")}";

        return SendAsync(
            email,
            "Bekræft din email",
            $"<p>Klik på linket for at bekræfte din email:</p><p><a href=\"{link}\">{link}</a></p>");
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        Task.CompletedTask; // Not used by the API-only Identity endpoints; see SendPasswordResetCodeAsync.

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode)
    {
        var link = $"{_appBaseUrl}/reset-password?email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(resetCode)}";

        return SendAsync(
            email,
            "Nulstil din adgangskode",
            $"<p>Klik på linket for at nulstille din adgangskode:</p><p><a href=\"{link}\">{link}</a></p><p>Hvis du ikke har bedt om dette, kan du ignorere denne email.</p>");
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
