using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/calendar-events")]
public class CalendarEventController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CalendarAccessService _calendarAccess;
    private readonly AppNotifier _notifier;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public CalendarEventController(AppDbContext context, CalendarAccessService calendarAccess, AppNotifier notifier)
    {
        _context = context;
        _calendarAccess = calendarAccess;
        _notifier = notifier;
    }

    private Task<int> GetOrCreateMyCalendarIdAsync() => _calendarAccess.GetOrCreateCalendarIdAsync(UserId);

    private static bool IsValidReminder(CalendarEvent calendarEvent)
    {
        if (calendarEvent.ReminderValue is null) return calendarEvent.ReminderUnit is null;
        if (calendarEvent.ReminderUnit is null) return false;

        return calendarEvent.ReminderUnit switch
        {
            ReminderPeriod.Hours => calendarEvent.ReminderValue is >= 1 and <= 24,
            ReminderPeriod.Days => calendarEvent.ReminderValue >= 1,
            _ => false
        };
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarEvent>>> GetByRange([FromQuery] DateOnly start, [FromQuery] DateOnly end)
    {
        var calendarId = await GetOrCreateMyCalendarIdAsync();
        return await _context.CalendarEvents
            .Where(e => e.CalendarId == calendarId && e.Date >= start && e.Date <= end)
            .OrderBy(e => e.Date).ThenBy(e => e.Time)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<CalendarEvent>> Create(CalendarEvent calendarEvent)
    {
        if (!IsValidReminder(calendarEvent)) return BadRequest("Ugyldig påmindelsesindstilling.");

        calendarEvent.CalendarId = await GetOrCreateMyCalendarIdAsync();
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();
        await _notifier.NotifyCalendarEventsChanged(calendarEvent.CalendarId);
        return CreatedAtAction(nameof(GetById), new { id = calendarEvent.Id }, calendarEvent);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CalendarEvent>> GetById(int id)
    {
        var calendarId = await GetOrCreateMyCalendarIdAsync();
        var item = await _context.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id && e.CalendarId == calendarId);
        if (item is null) return NotFound();
        return item;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CalendarEvent updatedEvent)
    {
        if (id != updatedEvent.Id) return BadRequest();
        if (!IsValidReminder(updatedEvent)) return BadRequest("Ugyldig påmindelsesindstilling.");

        var calendarId = await GetOrCreateMyCalendarIdAsync();
        var item = await _context.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id && e.CalendarId == calendarId);
        if (item is null) return NotFound();

        item.Title = updatedEvent.Title;
        item.Description = updatedEvent.Description;
        item.Date = updatedEvent.Date;
        item.Time = updatedEvent.Time;
        item.ReminderValue = updatedEvent.ReminderValue;
        item.ReminderUnit = updatedEvent.ReminderUnit;

        await _context.SaveChangesAsync();
        await _notifier.NotifyCalendarEventsChanged(calendarId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var calendarId = await GetOrCreateMyCalendarIdAsync();
        var item = await _context.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id && e.CalendarId == calendarId);
        if (item is null) return NotFound();

        _context.CalendarEvents.Remove(item);
        await _context.SaveChangesAsync();
        await _notifier.NotifyCalendarEventsChanged(calendarId);
        return NoContent();
    }
}
