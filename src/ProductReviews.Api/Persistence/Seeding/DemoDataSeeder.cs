using Microsoft.EntityFrameworkCore;
using ProductReviews.Api.Features.Catalog;
using ProductReviews.Api.Features.Reviews;
using ProductReviews.Api.Features.Votes;
using StrongTypes;

namespace ProductReviews.Api.Persistence.Seeding;

/// <summary>Idempotent demo seeding (ADR-0006): runs after migrations, does nothing
/// once products exist, and builds everything through the domain entities so seed
/// data satisfies the same invariants as user input.</summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(ReviewsDbContext dbContext, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var seedProduct in DemoCatalog.Products)
        {
            var product = new Product(
                seedProduct.Id,
                seedProduct.Slug.ToNonEmpty(),
                seedProduct.Name.ToNonEmpty(),
                seedProduct.Description.ToNonEmpty(),
                seedProduct.ImageUrl?.ToNonEmpty(),
                nowUtc.AddDays(-120));

            var ratings = new List<Rating>();
            foreach (var seedReview in seedProduct.Reviews)
            {
                var author = DemoCatalog.Authors.Single(a => a.Name == seedReview.Author);
                var createdAtUtc = nowUtc.AddDays(-seedReview.DaysAgo);

                var review = new Review(
                    product.Id,
                    author.Id,
                    author.Name.ToNonEmpty(),
                    Rating.Create(seedReview.Rating),
                    seedReview.Title.ToNonEmpty(),
                    seedReview.Body.ToNonEmpty(),
                    seedReview.Pros?.ToNonEmpty(),
                    seedReview.Cons?.ToNonEmpty(),
                    createdAtUtc);

                var votes = CreateVotes(review, author.Id, seedReview.Helpful, createdAtUtc);
                review.RefreshScore(votes);
                ratings.Add(review.Rating);

                dbContext.Reviews.Add(review);
                dbContext.ReviewVotes.AddRange(votes);
            }

            product.RefreshRatingSummary(ratings);
            dbContext.Products.Add(product);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<ReviewVote> CreateVotes(Review review, Guid reviewAuthorId, int helpful, DateTime reviewCreatedAtUtc)
    {
        // Voters rotate through the other seed authors — never the review's own
        // author, mirroring the "no voting on your own review" rule.
        var voters = DemoCatalog.Authors.Where(a => a.Id != reviewAuthorId).ToList();
        var voteCount = int.Min(int.Abs(helpful), voters.Count);

        return [.. voters
            .Take(voteCount)
            .Select((voter, index) => new ReviewVote(review.Id, voter.Id, helpful > 0, reviewCreatedAtUtc.AddHours(index + 1)))];
    }
}
