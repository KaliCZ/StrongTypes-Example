using FsCheck;
using FsCheck.Fluent;
using ProductReviews.Domain.Reviews;
using StrongTypes;
// FsCheck ships its own NonEmptyString test wrapper; ours is the StrongTypes one.
using NonEmptyString = StrongTypes.NonEmptyString;

namespace ProductReviews.Domain.Tests;

/// <summary>Project-specific arbitraries. Register together with the library's:
/// <c>[Properties(Arbitrary = [typeof(DomainGenerators), typeof(Generators)])]</c>
/// (<c>StrongTypes.FsCheck.Generators</c> covers NonEmptyString, Maybe, and friends).</summary>
public static class DomainGenerators
{
    public static Arbitrary<Rating> Rating { get; } =
        Gen.Choose(Reviews.Rating.MinimumStars, Reviews.Rating.MaximumStars)
            .Select(stars => Reviews.Rating.Create(stars))
            .ToArbitrary();

    /// <summary>The three-state edit parameter: ~30% skip (null), ~20% clear (None), ~50% set.</summary>
    public static Arbitrary<Maybe<NonEmptyString>?> NullableMaybeNonEmptyString { get; } =
        Gen.Frequency(
            (3, Gen.Constant<Maybe<NonEmptyString>?>(null)),
            (2, Gen.Constant<Maybe<NonEmptyString>?>(Maybe<NonEmptyString>.None)),
            (5, StrongTypes.FsCheck.Generators.NonEmptyString.Generator
                .Select(value => (Maybe<NonEmptyString>?)Maybe.Some(value))))
            .ToArbitrary();
}
