using ProductReviews.Api.Features.Reviews;
using StrongTypes;

namespace ProductReviews.Api.UnitTests;

internal static class ReviewTestData
{
    public static readonly DateTime CreatedAtUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime EditedAtUtc = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    public static Review CreateReview(NonEmptyString? pros = null, NonEmptyString? cons = null)
        => new(
            productId: 1,
            authorId: Guid.NewGuid(),
            authorDisplayName: "Alice Novak".ToNonEmpty(),
            rating: Rating.Create(4),
            title: "A solid product".ToNonEmpty(),
            body: "Does what it promises.".ToNonEmpty(),
            pros: pros,
            cons: cons,
            createdAtUtc: CreatedAtUtc);
}
