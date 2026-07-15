using Microsoft.EntityFrameworkCore;
using ProductReviews.Domain.Catalog;
using ProductReviews.Domain.Votes;
using StrongTypes;

namespace ProductReviews.Domain.Persistence;

/// <summary>The single source of truth for what a "complete" aggregate loads.
/// Each method pairs an Include chain with the record that proves it ran.</summary>
public static class CompleteQueries
{
    public static async Task<ProductWithReviews?> CompleteProductBySlug(
        this ReviewsDbContext dbContext, NonEmptyString slug, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(p => p.Reviews)
            .SingleOrDefaultAsync(p => p.Slug == slug, cancellationToken);
        return product is null ? null : ProductWithReviews.FromCompleteQuery(product);
    }

    public static async Task<ProductWithReviews?> CompleteProductById(
        this ReviewsDbContext dbContext, long productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(p => p.Reviews)
            .SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);
        return product is null ? null : ProductWithReviews.FromCompleteQuery(product);
    }

    public static async Task<ReviewWithVotes?> CompleteReviewById(
        this ReviewsDbContext dbContext, Guid reviewId, CancellationToken cancellationToken)
    {
        var review = await dbContext.Reviews
            .Include(r => r.Votes)
            .SingleOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        return review is null ? null : ReviewWithVotes.FromCompleteQuery(review);
    }
}
