using Microsoft.EntityFrameworkCore;
using ProductReviews.Api.Features.Catalog;
using ProductReviews.Api.Features.Reviews;
using ProductReviews.Api.Features.Votes;
using StrongTypes.EfCore;

namespace ProductReviews.Api.Persistence;

public sealed class ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // The library's built-ins are converted by UseStrongTypes(); our own
        // Rating wrapper needs this one registration, reusing the library's converter.
        configurationBuilder.Properties<Rating>()
            .HaveConversion<NumericStrongTypeValueConverter<Rating, int>>();
    }
}
