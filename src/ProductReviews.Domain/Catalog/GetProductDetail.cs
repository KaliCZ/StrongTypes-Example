using Microsoft.EntityFrameworkCore;
using ProductReviews.Domain.Persistence;
using StrongTypes;

namespace ProductReviews.Domain.Catalog;

/// <summary>ViewerReviewId is the signed-in viewer's own review of this product, when they have one.</summary>
public sealed record ProductDetailModel(Product Product, Guid? ViewerReviewId);

public sealed class GetProductDetailHandler(ReviewsDbContext dbContext)
{
    /// <summary>Null means the product does not exist.</summary>
    public async Task<ProductDetailModel?> HandleAsync(NonEmptyString slug, Guid? viewerId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug, cancellationToken);
        if (product is null)
        {
            return null;
        }

        Guid? viewerReviewId = viewerId is { } viewer
            ? await dbContext.Reviews
                .Where(review => review.ProductId == product.Id && review.AuthorId == viewer)
                .Select(review => (Guid?)review.Id)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return new ProductDetailModel(product, viewerReviewId);
    }
}
