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
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanRecipe> MealPlanRecipes => Set<MealPlanRecipe>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<ReminderDelivery> ReminderDeliveries => Set<ReminderDelivery>();
    public DbSet<SharedRecipe> SharedRecipes => Set<SharedRecipe>();
    public DbSet<SharedRecipeIngredient> SharedRecipeIngredients => Set<SharedRecipeIngredient>();
    public DbSet<SharedRecipeTag> SharedRecipeTags => Set<SharedRecipeTag>();
    public DbSet<ForumSearchLog> ForumSearchLogs => Set<ForumSearchLog>();
    public DbSet<KnownItemRequest> KnownItemRequests => Set<KnownItemRequest>();
    public DbSet<CalendarEventException> CalendarEventExceptions => Set<CalendarEventException>();
    public DbSet<EmailReminderDelivery> EmailReminderDeliveries => Set<EmailReminderDelivery>();
    public DbSet<DigestPreference> DigestPreferences => Set<DigestPreference>();
    public DbSet<DigestDelivery> DigestDeliveries => Set<DigestDelivery>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<PrivacyPolicyAcceptance> PrivacyPolicyAcceptances => Set<PrivacyPolicyAcceptance>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanGuest> PlanGuests => Set<PlanGuest>();
    public DbSet<PlanTodo> PlanTodos => Set<PlanTodo>();
    public DbSet<PersonalNote> PersonalNotes => Set<PersonalNote>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetMember> BudgetMembers => Set<BudgetMember>();
    public DbSet<BudgetInvite> BudgetInvites => Set<BudgetInvite>();
    public DbSet<BudgetFixedEntry> BudgetFixedEntries => Set<BudgetFixedEntry>();
    public DbSet<BudgetVariableCategory> BudgetVariableCategories => Set<BudgetVariableCategory>();

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

        builder.Entity<PlanGuest>()
            .HasOne<Plan>()
            .WithMany()
            .HasForeignKey(g => g.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlanGuest>()
            .HasIndex(g => new { g.PlanId, g.UserId })
            .IsUnique();

        builder.Entity<PlanTodo>()
            .HasOne<Plan>()
            .WithMany()
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

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

        builder.Entity<BudgetMember>()
            .HasOne<Budget>()
            .WithMany()
            .HasForeignKey(m => m.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BudgetMember>()
            .HasIndex(m => new { m.BudgetId, m.UserId })
            .IsUnique();

        builder.Entity<BudgetFixedEntry>()
            .HasOne<Budget>()
            .WithMany()
            .HasForeignKey(e => e.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BudgetVariableCategory>()
            .HasOne<Budget>()
            .WithMany()
            .HasForeignKey(c => c.BudgetId)
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

        builder.Entity<MealPlanRecipe>()
            .HasOne<MealPlan>()
            .WithMany()
            .HasForeignKey(mpr => mpr.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MealPlanRecipe>()
            .HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(mpr => mpr.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PushSubscription>()
            .HasIndex(s => s.Token)
            .IsUnique();

        builder.Entity<ReminderDelivery>()
            .HasOne<CalendarEvent>()
            .WithMany()
            .HasForeignKey(d => d.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ReminderDelivery>()
            .HasIndex(d => new { d.CalendarEventId, d.OccurrenceDate, d.UserId })
            .IsUnique();

        builder.Entity<CalendarEventException>()
            .HasOne<CalendarEvent>()
            .WithMany()
            .HasForeignKey(e => e.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CalendarEventException>()
            .HasIndex(e => new { e.CalendarEventId, e.ExceptionDate })
            .IsUnique();

        builder.Entity<EmailReminderDelivery>()
            .HasOne<CalendarEvent>()
            .WithMany()
            .HasForeignKey(d => d.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmailReminderDelivery>()
            .HasIndex(d => new { d.CalendarEventId, d.OccurrenceDate, d.UserId })
            .IsUnique();

        builder.Entity<DigestPreference>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        builder.Entity<DigestDelivery>()
            .HasIndex(d => new { d.UserId, d.Frequency, d.SentDate })
            .IsUnique();

        builder.Entity<BlockedUser>()
            .HasIndex(b => new { b.BlockerUserId, b.BlockedUserId })
            .IsUnique();

        builder.Entity<SharedRecipeIngredient>()
            .HasOne<SharedRecipe>()
            .WithMany()
            .HasForeignKey(i => i.SharedRecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SharedRecipeTag>()
            .HasOne<SharedRecipe>()
            .WithMany()
            .HasForeignKey(t => t.SharedRecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}