using ProductReviews.Api.Persistence;
using StrongTypes;

namespace ProductReviews.Api.Features.Reviews;

public enum SubmitReviewError
{
    ProductNotFound,
    AlreadyReviewed,
}

public sealed class SubmitReviewHandler(ReviewsDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<Result<Review, SubmitReviewError>> HandleAsync(
        NonEmptyString productSlug,
        ReviewAuthor author,
        Rating rating,
        NonEmptyString title,
        NonEmptyString body,
        NonEmptyString? pros,
        NonEmptyString? cons,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.CompleteProductBySlug(productSlug, cancellationToken);
        if (product is null)
        {
            return SubmitReviewError.ProductNotFound;
        }
        if (product.Reviews.Any(review => review.AuthorId == author.AuthorId))
        {
            return SubmitReviewError.AlreadyReviewed;
        }

        var review = new Review(
            product.Product.Id,
            author.AuthorId,
            author.DisplayName,
            rating,
            title,
            body,
            pros,
            cons,
            timeProvider.GetUtcNow().UtcDateTime);
        dbContext.Reviews.Add(review);

        var afterSubmit = product with { Reviews = [.. product.Reviews, review] };
        afterSubmit.RefreshRatingSummary();

        await dbContext.SaveChangesAsync(cancellationToken);
        return review;
    }
}
