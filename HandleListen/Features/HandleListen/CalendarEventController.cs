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
    public async Task<ActionResult<IEnumerable<CalendarEventOccurrenceDto>>> GetByRange([FromQuery] DateOnly start, [FromQuery] DateOnly end)
    {
        var calendarId = await GetOrCreateMyCalendarIdAsync();
        var events = await _context.CalendarEvents.Where(e => e.CalendarId == calendarId).ToListAsync();

        var recurringIds = events.Where(e => e.Recurrence is not null).Select(e => e.Id).ToList();
        var exceptionsByEvent = (await _context.CalendarEventExceptions
                .Where(x => recurringIds.Contains(x.CalendarEventId))
                .ToListAsync())
            .GroupBy(x => x.CalendarEventId)
            .ToDictionary(g => g.Key, g => (ISet<DateOnly>)g.Select(x => x.ExceptionDate).ToHashSet());

        var result = RecurringEventService.ExpandEvents(events, start, end, exceptionsByEvent)
            .Select(x => new CalendarEventOccurrenceDto(
                x.Event.Id, x.Event.Title, x.Event.Description, x.Date, x.Event.Time,
                x.Event.ReminderValue, x.Event.ReminderUnit, x.Event.Recurrence,
                x.Event.PushReminderEnabled, x.Event.EmailReminderEnabled))
            .OrderBy(e => e.Date).ThenBy(e => e.Time)
            .ToList();

        return result;
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
        item.Recurrence = updatedEvent.Recurrence;
        item.PushReminderEnabled = updatedEvent.PushReminderEnabled;
        item.EmailReminderEnabled = updatedEvent.EmailReminderEnabled;

        await _context.SaveChangesAsync();
        await _notifier.NotifyCalendarEventsChanged(calendarId);
        return NoContent();
    }

    // Deletes the whole series (or the only occurrence, for a non-recurring event).
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

    // Skips one computed occurrence of a recurring series without touching the series itself.
    [HttpDelete("{id}/occurrences/{date}")]
    public async Task<IActionResult> DeleteOccurrence(int id, DateOnly date)
    {
        var calendarId = await GetOrCreateMyCalendarIdAsync();
        var item = await _context.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id && e.CalendarId == calendarId);
        if (item is null) return NotFound();
        if (item.Recurrence is null) return BadRequest("Dette event gentager sig ikke - slet hele eventet i stedet.");

        var exists = await _context.CalendarEventExceptions.AnyAsync(x => x.CalendarEventId == id && x.ExceptionDate == date);
        if (!exists)
        {
            _context.CalendarEventExceptions.Add(new CalendarEventException { CalendarEventId = id, ExceptionDate = date });
            await _context.SaveChangesAsync();
        }
        await _notifier.NotifyCalendarEventsChanged(calendarId);
        return NoContent();
    }
}
