using FsCheck.Xunit;
using ProductReviews.Domain.Reviews;
using StrongTypes.FsCheck;
using Xunit;

namespace ProductReviews.Domain.Tests;

[Properties(Arbitrary = [typeof(DomainGenerators), typeof(Generators)])]
public sealed class RatingTests
{
    [Property]
    public bool TryCreate_AcceptsExactlyOneThroughFive(int value)
        => (Rating.TryCreate(value) is not null) == (value is >= 1 and <= 5);

    [Property]
    public bool Value_RoundTripsThroughCreate(Rating rating)
        => Rating.Create(rating.Value) == rating;

    [Property]
    public bool Ordering_FollowsTheUnderlyingValue(Rating left, Rating right)
        => left.CompareTo(right) == left.Value.CompareTo(right.Value);

    [Fact]
    public void Default_IsAValidOneStarRating()
        => Assert.Equal(1, default(Rating).Value);

    [Fact]
    public void Create_OutOfRange_Throws()
        => Assert.Throws<ArgumentException>(() => Rating.Create(6));
}
