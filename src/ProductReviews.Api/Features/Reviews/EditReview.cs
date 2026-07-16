using Microsoft.EntityFrameworkCore;
using ProductReviews.Api.Persistence;
using StrongTypes;

namespace ProductReviews.Api.Features.Reviews;

public enum EditReviewError
{
    ReviewNotFound,
    NotYourReview,
}

public sealed class EditReviewHandler(ReviewsDbContext dbContext, TimeProvider timeProvider)
{
    /// <summary>The Maybe&lt;T&gt; showcase (ADR-0002, §5 of the technical requirements):
    /// null leaves a field unchanged; for the clearable optionals, None clears and Some replaces.</summary>
    public async Task<Result<Review, EditReviewError>> HandleAsync(
        Guid reviewId,
        Guid editorId,
        Rating? rating,
        NonEmptyString? title,
        NonEmptyString? body,
        Maybe<NonEmptyString>? pros,
        Maybe<NonEmptyString>? cons,
        CancellationToken cancellationToken)
    {
        var review = await dbContext.Reviews.SingleOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return EditReviewError.ReviewNotFound;
        }
        if (review.AuthorId != editorId)
        {
            return EditReviewError.NotYourReview;
        }

        var ratingChanged = rating is { } newRating && newRating != review.Rating;
        review.ApplyEdit(rating, title, body, pros, cons, timeProvider.GetUtcNow().UtcDateTime);

        if (ratingChanged)
        {
            var product = await dbContext.CompleteProductById(review.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Product {review.ProductId} missing for review {review.Id}.");
            product.RefreshRatingSummary();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return review;
    }
}
