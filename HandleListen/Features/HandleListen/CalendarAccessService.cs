using Microsoft.EntityFrameworkCore;

public class CalendarAccessService
{
    private readonly AppDbContext _context;

    public CalendarAccessService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetOrCreateCalendarIdAsync(string userId)
    {
        var existingCalendarId = await _context.CalendarMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.CalendarId)
            .FirstOrDefaultAsync();
        if (existingCalendarId != 0) return existingCalendarId;

        var calendar = new Calendar();
        _context.Calendars.Add(calendar);
        await _context.SaveChangesAsync();

        _context.CalendarMembers.Add(new CalendarMember { CalendarId = calendar.Id, UserId = userId });
        await _context.SaveChangesAsync();

        return calendar.Id;
    }
}
