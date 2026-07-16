using FsCheck.Xunit;
using ProductReviews.Api.Features.Catalog;
using ProductReviews.Api.Features.Reviews;
using StrongTypes;
using StrongTypes.FsCheck;
using Xunit;

namespace ProductReviews.Api.UnitTests;

[Properties(Arbitrary = [typeof(DomainGenerators), typeof(Generators)])]
public sealed class ProductRatingSummaryTests
{
    private static Product CreateProduct()
        => new(
            id: 1,
            slug: "test-product".ToNonEmpty(),
            name: "Test Product".ToNonEmpty(),
            description: "A product for tests.".ToNonEmpty(),
            imageUrl: null,
            createdAtUtc: ReviewTestData.CreatedAtUtc);

    [Property]
    public void CountAndAverage_MatchTheRatings(Rating[] ratings)
    {
        var product = CreateProduct();

        product.RefreshRatingSummary(ratings);

        Assert.Equal(ratings.Length, product.ReviewCount.Value);
        if (ratings.Length == 0)
        {
            Assert.Null(product.AverageRating);
        }
        else
        {
            Assert.NotNull(product.AverageRating);
            Assert.Equal(ratings.Average(rating => (double)rating.Value), product.AverageRating.Value, precision: 10);
            Assert.InRange(product.AverageRating.Value, Rating.MinimumStars, Rating.MaximumStars);
        }
    }

    [Fact]
    public void NoReviews_MeansNotYetRated_NeverZero()
    {
        var product = CreateProduct();

        product.RefreshRatingSummary([Rating.Create(3)]);
        product.RefreshRatingSummary([]);

        Assert.Null(product.AverageRating);
        Assert.Equal(0, product.ReviewCount.Value);
    }
}
