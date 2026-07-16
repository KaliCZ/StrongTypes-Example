using Microsoft.EntityFrameworkCore;
using ProductReviews.Api.Persistence;
using ProductReviews.Api.Persistence.Seeding;
using StrongTypes.EfCore;

namespace ProductReviews.Api.Infrastructure;

public static class Persistence
{
    /// <summary>Matches the database resource name in the AppHost.</summary>
    public const string DatabaseResourceName = "productreviews";

    public static void Configure(WebApplicationBuilder builder)
        => builder.AddNpgsqlDbContext<ReviewsDbContext>(
            DatabaseResourceName,
            // The explicit "database" check in Health.cs is the one source of DB health.
            configureSettings: settings => settings.DisableHealthChecks = true,
            configureDbContextOptions: options => options.UseStrongTypes());

    /// <summary>Development-only convenience (ADR-0004): production applies migrations
    /// from the deploy pipeline and never seeds.</summary>
    public static async Task MigrateAndSeedAsync(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        await dbContext.Database.MigrateAsync();
        await DemoDataSeeder.SeedAsync(dbContext, scope.ServiceProvider.GetRequiredService<TimeProvider>(), CancellationToken.None);
    }
}
