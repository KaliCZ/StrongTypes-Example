using Microsoft.EntityFrameworkCore;
using ProductReviews.Api.Persistence;
using StrongTypes;

namespace ProductReviews.Api.Features.Reviews;

/// <summary>A review as one specific viewer sees it: with whether it is their own
/// and which way they voted on it. Constructed only by the page query, so both
/// viewer facts are guaranteed to have been loaded.</summary>
public sealed record ReviewWithViewerContext(Review Review, bool Mine, bool? ViewerVote);

public sealed record ReviewsPage(
    IReadOnlyList<ReviewWithViewerContext> Reviews,
    NonNegative<int> TotalCount,
    Positive<int> Page,
    Positive<int> PageSize);

public sealed class GetReviewsPageHandler(ReviewsDbContext dbContext)
{
    public const int MaximumPageSize = 50;

    /// <summary>Null means the product does not exist.</summary>
    public async Task<ReviewsPage?> HandleAsync(
        NonEmptyString slug,
        ReviewSort sort,
        IReadOnlyCollection<Rating> ratingFilter,
        Positive<int> page,
        Positive<int> pageSize,
        Guid? viewerId,
        CancellationToken cancellationToken)
    {
        var productId = await dbContext.Products
            .Where(product => product.Slug == slug)
            .Select(product => (long?)product.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (productId is null)
        {
            return null;
        }

        var reviews = dbContext.Reviews
            .AsNoTracking()
            .Where(review => review.ProductId == productId);

        if (ratingFilter.Count > 0)
        {
            reviews = reviews.Where(review => ratingFilter.Contains(review.Rating));
        }

        reviews = sort switch
        {
            ReviewSort.MostHelpful => reviews
                .OrderByDescending(review => review.Score)
                .ThenByDescending(review => review.Id),
            ReviewSort.Newest => reviews
                .OrderByDescending(review => review.Id),
            ReviewSort.HighestRating => reviews
                .OrderByDescending(review => review.Rating)
                .ThenByDescending(review => review.Score)
                .ThenByDescending(review => review.Id),
            ReviewSort.LowestRating => reviews
                .OrderBy(review => review.Rating)
                .ThenByDescending(review => review.Score)
                .ThenByDescending(review => review.Id),
        };

        var totalCount = await reviews.CountAsync(cancellationToken);

        var effectivePageSize = int.Min(pageSize.Value, MaximumPageSize);
        var pageQuery = reviews
            .Skip((page.Value - 1) * effectivePageSize)
            .Take(effectivePageSize);

        var pageItems = viewerId is { } viewer
            ? await pageQuery
                .Select(review => new ReviewWithViewerContext(
                    review,
                    review.AuthorId == viewer,
                    review.Votes
                        .Where(vote => vote.VoterId == viewer)
                        .Select(vote => (bool?)vote.IsUpvote)
                        .FirstOrDefault()))
                .ToListAsync(cancellationToken)
            : await pageQuery
                .Select(review => new ReviewWithViewerContext(review, false, null))
                .ToListAsync(cancellationToken);

        return new ReviewsPage(pageItems, totalCount.ToNonNegative(), page, effectivePageSize.ToPositive());
    }
}
