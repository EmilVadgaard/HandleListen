using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListGuest> ShoppingListGuests => Set<ShoppingListGuest>();

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
    }
}