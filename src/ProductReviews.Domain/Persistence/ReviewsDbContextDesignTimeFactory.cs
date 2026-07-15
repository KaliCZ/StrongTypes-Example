using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StrongTypes.EfCore;

namespace ProductReviews.Domain.Persistence;

/// <summary>For `dotnet ef` only. The connection string is never opened during
/// `migrations add` — it just satisfies the provider registration.</summary>
public sealed class ReviewsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ReviewsDbContext>
{
    public ReviewsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReviewsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=productreviews-design-time").UseStrongTypes();
        return new ReviewsDbContext(optionsBuilder.Options);
    }
}
