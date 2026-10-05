using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CalendarAccessService _calendarAccess;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public CalendarController(AppDbContext context, CalendarAccessService calendarAccess, AppNotifier notifier)
    {
        _context = context;
        _calendarAccess = calendarAccess;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<ActionResult<CalendarIdDto>> GetMine()
    {
        var calendarId = await _calendarAccess.GetOrCreateCalendarIdAsync(UserId);
        return new CalendarIdDto(calendarId);
    }

    [HttpPost("leave")]
    public async Task<IActionResult> Leave()
    {
        var calendarId = await _calendarAccess.GetOrCreateCalendarIdAsync(UserId);

        var otherMemberIds = await _context.CalendarMembers
            .Where(m => m.CalendarId == calendarId && m.UserId != UserId)
            .Select(m => m.UserId)
            .ToListAsync();
        if (otherMemberIds.Count == 0) return BadRequest("Du deler ikke din kalender med nogen.");

        var myMember = await _context.CalendarMembers
            .FirstAsync(m => m.CalendarId == calendarId && m.UserId == UserId);

        var newCalendar = new Calendar();
        _context.Calendars.Add(newCalendar);
        await _context.SaveChangesAsync();

        var existingEvents = await _context.CalendarEvents
            .Where(e => e.CalendarId == calendarId)
            .ToListAsync();
        foreach (var e in existingEvents)
        {
            _context.CalendarEvents.Add(new CalendarEvent
            {
                CalendarId = newCalendar.Id,
                Title = e.Title,
                Description = e.Description,
                Date = e.Date,
                Time = e.Time,
                ReminderValue = e.ReminderValue,
                ReminderUnit = e.ReminderUnit
            });
        }

        myMember.CalendarId = newCalendar.Id;
        await _context.SaveChangesAsync();

        await _notifier.NotifyCalendarChanged(otherMemberIds.Append(UserId));
        return NoContent();
    }

    [HttpGet("members")]
    public async Task<ActionResult<IEnumerable<CalendarMemberDto>>> GetMembers()
    {
        var calendarId = await _calendarAccess.GetOrCreateCalendarIdAsync(UserId);
        return await _context.CalendarMembers
            .Where(m => m.CalendarId == calendarId)
            .Join(_context.Users, m => m.UserId, u => u.Id, (m, u) => new CalendarMemberDto(u.Id, u.Email!))
            .ToListAsync();
    }

    [HttpGet("invites")]
    public async Task<ActionResult<IEnumerable<CalendarInviteDto>>> GetInvites()
    {
        var invites = await _context.CalendarInvites
            .Where(i => i.FromUserId == UserId || i.ToUserId == UserId)
            .ToListAsync();

        var userIds = invites.SelectMany(i => new[] { i.FromUserId, i.ToUserId }).Distinct().ToList();
        var emails = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email!);

        return invites
            .Select(i => new CalendarInviteDto(
                i.Id,
                emails.GetValueOrDefault(i.FromUserId, ""),
                emails.GetValueOrDefault(i.ToUserId, ""),
                i.ToUserId == UserId,
                i.CreatedAt))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }

    [HttpPost("invites")]
    public async Task<ActionResult<CalendarInviteDto>> CreateInvite(InviteRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var toUser = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (toUser is null) return NotFound("Ingen bruger fundet med den email.");
        if (toUser.Id == UserId) return BadRequest("Du kan ikke invitere dig selv.");

        var myCalendarId = await _calendarAccess.GetOrCreateCalendarIdAsync(UserId);
        var theirCalendarId = await _calendarAccess.GetOrCreateCalendarIdAsync(toUser.Id);
        if (myCalendarId == theirCalendarId) return Conflict("I deler allerede kalender.");

        var alreadyInvited = await _context.CalendarInvites.AnyAsync(i =>
            (i.FromUserId == UserId && i.ToUserId == toUser.Id) ||
            (i.FromUserId == toUser.Id && i.ToUserId == UserId));
        if (alreadyInvited) return Conflict("Der findes allerede en invitation mellem jer.");

        var invite = new CalendarInvite { FromUserId = UserId, ToUserId = toUser.Id };
        _context.CalendarInvites.Add(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyCalendarChanged([toUser.Id]);

        var myEmail = await _context.Users.Where(u => u.Id == UserId).Select(u => u.Email!).FirstAsync();
        return new CalendarInviteDto(invite.Id, myEmail, toUser.Email!, false, invite.CreatedAt);
    }

    [HttpPost("invites/{id}/accept")]
    public async Task<IActionResult> AcceptInvite(int id)
    {
        var invite = await _context.CalendarInvites.FirstOrDefaultAsync(i => i.Id == id && i.ToUserId == UserId);
        if (invite is null) return NotFound();

        var fromCalendarId = await _calendarAccess.GetOrCreateCalendarIdAsync(invite.FromUserId);
        var toCalendarId = await _calendarAccess.GetOrCreateCalendarIdAsync(UserId);

        var affectedUserIds = new List<string>();

        if (fromCalendarId != toCalendarId)
        {
            var members = await _context.CalendarMembers
                .Where(m => m.CalendarId == fromCalendarId || m.CalendarId == toCalendarId)
                .ToListAsync();
            affectedUserIds.AddRange(members.Select(m => m.UserId));

            var mergedCalendar = new Calendar();
            _context.Calendars.Add(mergedCalendar);
            await _context.SaveChangesAsync();

            var events = await _context.CalendarEvents
                .Where(e => e.CalendarId == fromCalendarId || e.CalendarId == toCalendarId)
                .ToListAsync();
            foreach (var calendarEvent in events) calendarEvent.CalendarId = mergedCalendar.Id;

            foreach (var member in members) member.CalendarId = mergedCalendar.Id;

            await _context.SaveChangesAsync();

            var oldCalendars = await _context.Calendars
                .Where(c => c.Id == fromCalendarId || c.Id == toCalendarId)
                .ToListAsync();
            _context.Calendars.RemoveRange(oldCalendars);
        }

        _context.CalendarInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyCalendarChanged(affectedUserIds);
        return NoContent();
    }

    [HttpPost("invites/{id}/decline")]
    public async Task<IActionResult> DeclineInvite(int id)
    {
        var invite = await _context.CalendarInvites.FirstOrDefaultAsync(i => i.Id == id && i.ToUserId == UserId);
        if (invite is null) return NotFound();

        _context.CalendarInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyCalendarChanged([invite.FromUserId]);
        return NoContent();
    }

    [HttpDelete("invites/{id}")]
    public async Task<IActionResult> CancelInvite(int id)
    {
        var invite = await _context.CalendarInvites.FirstOrDefaultAsync(i => i.Id == id && i.FromUserId == UserId);
        if (invite is null) return NotFound();

        _context.CalendarInvites.Remove(invite);
        await _context.SaveChangesAsync();

        await _notifier.NotifyCalendarChanged([invite.ToUserId]);
        return NoContent();
    }
}
