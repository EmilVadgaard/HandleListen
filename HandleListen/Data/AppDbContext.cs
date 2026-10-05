using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListGuest> ShoppingListGuests => Set<ShoppingListGuest>();
    public DbSet<Calendar> Calendars => Set<Calendar>();
    public DbSet<CalendarMember> CalendarMembers => Set<CalendarMember>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<CalendarInvite> CalendarInvites => Set<CalendarInvite>();
    public DbSet<KnownItem> KnownItems => Set<KnownItem>();
    public DbSet<KnownItemAlias> KnownItemAliases => Set<KnownItemAlias>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeTag> RecipeTags => Set<RecipeTag>();
    public DbSet<RecipeBook> RecipeBooks => Set<RecipeBook>();
    public DbSet<RecipeBookMember> RecipeBookMembers => Set<RecipeBookMember>();
    public DbSet<RecipeBookInvite> RecipeBookInvites => Set<RecipeBookInvite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ShoppingListGuest>()
            .HasOne<ShoppingList>()
            .WithMany()
            .HasForeignKey(g => g.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ShoppingListGuest>()
            .HasIndex(g => new { g.ShoppingListId, g.UserId })
            .IsUnique();

        builder.Entity<CalendarMember>()
            .HasOne<Calendar>()
            .WithMany()
            .HasForeignKey(m => m.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CalendarMember>()
            .HasIndex(m => new { m.CalendarId, m.UserId })
            .IsUnique();

        builder.Entity<CalendarEvent>()
            .HasOne<Calendar>()
            .WithMany()
            .HasForeignKey(e => e.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<KnownItemAlias>()
            .HasOne<KnownItem>()
            .WithMany()
            .HasForeignKey(a => a.KnownItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<KnownItemAlias>()
            .HasIndex(a => a.Alias)
            .IsUnique();

        builder.Entity<RecipeIngredient>()
            .HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RecipeTag>()
            .HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(t => t.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RecipeBookMember>()
            .HasOne<RecipeBook>()
            .WithMany()
            .HasForeignKey(m => m.RecipeBookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RecipeBookMember>()
            .HasIndex(m => new { m.RecipeBookId, m.UserId })
            .IsUnique();
    }
}