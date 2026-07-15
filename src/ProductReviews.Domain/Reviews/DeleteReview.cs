using Microsoft.EntityFrameworkCore;
using ProductReviews.Domain.Persistence;

namespace ProductReviews.Domain.Reviews;

public enum DeleteReviewError
{
    ReviewNotFound,
    NotYourReview,
}

public sealed class DeleteReviewHandler(ReviewsDbContext dbContext)
{
    /// <summary>Null result means success — there is nothing to return, so no Result wrapper.</summary>
    public async Task<DeleteReviewError?> HandleAsync(Guid reviewId, Guid authorId, CancellationToken cancellationToken)
    {
        var review = await dbContext.Reviews.SingleOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return DeleteReviewError.ReviewNotFound;
        }
        if (review.AuthorId != authorId)
        {
            return DeleteReviewError.NotYourReview;
        }

        dbContext.Reviews.Remove(review);

        var product = await dbContext.CompleteProductById(review.ProductId, cancellationToken)
            ?? throw new InvalidOperationException($"Product {review.ProductId} missing for review {review.Id}.");
        var afterDeletion = product with { Reviews = [.. product.Reviews.Where(r => r.Id != review.Id)] };
        afterDeletion.RefreshRatingSummary();

        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }
}
