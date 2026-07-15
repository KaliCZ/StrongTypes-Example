using ProductReviews.Domain.Reviews;
using StrongTypes;

namespace ProductReviews.Domain.Catalog;

public sealed class Product
{
    private Product()
    {
        Slug = null!;
        Name = null!;
        Description = null!;
    }

    public Product(long id, NonEmptyString slug, NonEmptyString name, NonEmptyString description, NonEmptyString? imageUrl, DateTime createdAtUtc)
    {
        Id = id;
        Slug = slug;
        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        CreatedAtUtc = createdAtUtc;
    }

    // Product ids come from the upstream catalog; this application never generates them.
    public long Id { get; private set; }

    public NonEmptyString Slug { get; private set; }

    public NonEmptyString Name { get; private set; }

    public NonEmptyString Description { get; private set; }

    public NonEmptyString? ImageUrl { get; private set; }

    public NonNegative<int> ReviewCount { get; private set; }

    /// <summary>Null until the first review exists — a product is "not yet rated", never rated zero.</summary>
    public double? AverageRating { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public ICollection<Review> Reviews { get; } = [];

    public void RefreshRatingSummary(IReadOnlyCollection<Rating> currentRatings)
    {
        ReviewCount = currentRatings.Count.ToNonNegative();
        AverageRating = currentRatings.Count == 0 ? null : currentRatings.Average(rating => (double)rating.Value);
    }
}
