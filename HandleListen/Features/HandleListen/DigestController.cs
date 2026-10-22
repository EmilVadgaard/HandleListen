using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/digest")]
public class DigestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DigestBuilderService _builder;
    private readonly CalendarEmailSender _emailSender;
    private readonly UserManager<IdentityUser> _userManager;
    private static readonly TimeZoneInfo DanishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public DigestController(AppDbContext context, DigestBuilderService builder, CalendarEmailSender emailSender, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _builder = builder;
        _emailSender = emailSender;
        _userManager = userManager;
    }

    [HttpGet("preference")]
    public async Task<ActionResult<DigestPreferenceDto>> GetPreference()
    {
        var preference = await _context.DigestPreferences.FirstOrDefaultAsync(p => p.UserId == UserId);
        return new DigestPreferenceDto(preference?.Frequency);
    }

    [HttpPut("preference")]
    public async Task<IActionResult> SetPreference(SetDigestPreferenceRequest request)
    {
        var preference = await _context.DigestPreferences.FirstOrDefaultAsync(p => p.UserId == UserId);

        if (request.Frequency is null)
        {
            if (preference is not null) _context.DigestPreferences.Remove(preference);
        }
        else if (preference is null)
        {
            _context.DigestPreferences.Add(new DigestPreference { UserId = UserId, Frequency = request.Frequency.Value });
        }
        else
        {
            preference.Frequency = request.Frequency.Value;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("send-now")]
    public async Task<IActionResult> SendNow(SendDigestNowRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.Email is null) return BadRequest("Ingen email registreret på kontoen.");

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DanishTimeZone));
        var (from, to) = DigestBuilderService.GetRange(request.Frequency, today);
        var events = await _builder.BuildEventListAsync(UserId, from, to);

        if (events.Count == 0) return Ok(new { sent = false });

        await _emailSender.SendDigestEmailAsync(user.Email, DigestEmailBackgroundService.SubjectFor(request.Frequency), DigestEmailBackgroundService.IntroFor(request.Frequency), events);
        return Ok(new { sent = true });
    }
}
