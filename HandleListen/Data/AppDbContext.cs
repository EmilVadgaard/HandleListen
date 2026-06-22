using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    // Senere: public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
}