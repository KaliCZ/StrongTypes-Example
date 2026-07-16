using StrongTypes;

namespace ProductReviews.Api.Features.Catalog;

public sealed record ProductSummaryResponse(
    long Id,
    NonEmptyString Slug,
    NonEmptyString Name,
    NonEmptyString? ImageUrl,
    NonNegative<int> ReviewCount,
    double? AverageRating)
{
    public static ProductSummaryResponse From(Product product)
        => new(product.Id, product.Slug, product.Name, product.ImageUrl, product.ReviewCount, product.AverageRating);
}

public sealed record ProductDetailResponse(
    long Id,
    NonEmptyString Slug,
    NonEmptyString Name,
    NonEmptyString Description,
    NonEmptyString? ImageUrl,
    NonNegative<int> ReviewCount,
    double? AverageRating,
    Guid? MyReviewId)
{
    public static ProductDetailResponse From(ProductDetailModel model)
        => new(
            model.Product.Id,
            model.Product.Slug,
            model.Product.Name,
            model.Product.Description,
            model.Product.ImageUrl,
            model.Product.ReviewCount,
            model.Product.AverageRating,
            model.ViewerReviewId);
}
