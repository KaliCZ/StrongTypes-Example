using ProductReviews.Domain.Reviews;

namespace ProductReviews.Domain.Catalog;

/// <summary>Proof-of-loading aggregate: it can only be constructed with the product's
/// reviews in hand, so code holding one never meets an unloaded navigation.
/// Construct via <c>CompleteQueries</c>, which owns the matching Include chain.</summary>
public sealed record ProductWithReviews(Product Product, IReadOnlyList<Review> Reviews)
{
    public static ProductWithReviews FromCompleteQuery(Product product)
        => new(product, [.. product.Reviews]);

    public void RefreshRatingSummary()
        => Product.RefreshRatingSummary([.. Reviews.Select(review => review.Rating)]);
}
