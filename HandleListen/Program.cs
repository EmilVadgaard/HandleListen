using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

const string allowFrontend = "AllowFrontend";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
    .AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddHttpClient<IEmailSender<IdentityUser>, ResendEmailSender>();

builder.Services.AddAuthentication();

// SignalR clients can't set an Authorization header on the WebSocket handshake,
// so accept the token from the query string for requests to the hub.
builder.Services.Configure<Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenOptions>(
    IdentityConstants.BearerScheme, options =>
{
    options.Events = new Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<AppNotifier>();
builder.Services.AddScoped<CalendarAccessService>();
builder.Services.AddScoped<RecipeBookAccessService>();
builder.Services.AddScoped<KnownItemLookupService>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowFrontend,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Owner", "Moderator" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var ownerEmail = builder.Configuration["Admin:OwnerEmail"];
    if (!string.IsNullOrEmpty(ownerEmail))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var owner = await userManager.FindByEmailAsync(ownerEmail);
        if (owner is not null && !await userManager.IsInRoleAsync(owner, "Owner"))
        {
            await userManager.AddToRoleAsync(owner, "Owner");
        }
    }

    if (!db.KnownItems.Any())
    {
        foreach (var (name, category) in KnownItemSeedData.Entries)
        {
            var item = new KnownItem { CanonicalName = name, Category = category };
            db.KnownItems.Add(item);
            db.SaveChanges();
            db.KnownItemAliases.Add(new KnownItemAlias { KnownItemId = item.Id, Alias = name.ToLowerInvariant() });
        }
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(allowFrontend);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGroup("/api/auth").MapIdentityApi<IdentityUser>();
app.MapHub<ShoppingListHub>("/hubs/shopping-list");

app.Run();
