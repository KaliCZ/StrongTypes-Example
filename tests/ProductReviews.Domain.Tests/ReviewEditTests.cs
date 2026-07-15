using FsCheck.Xunit;
using ProductReviews.Domain.Reviews;
using StrongTypes;
using StrongTypes.FsCheck;
using Xunit;

namespace ProductReviews.Domain.Tests;

/// <summary>The PATCH contract of Review.ApplyEdit: null leaves a field unchanged;
/// for the clearable optionals, Maybe.None clears and Maybe.Some replaces.</summary>
[Properties(Arbitrary = [typeof(DomainGenerators), typeof(Generators)])]
public sealed class ReviewEditTests
{
    [Fact]
    public void AllNulls_ChangeNothing_ButMarkTheReviewEdited()
    {
        var review = ReviewTestData.CreateReview(pros: "Great battery".ToNonEmpty());
        var originalRating = review.Rating;
        var originalTitle = review.Title;
        var originalBody = review.Body;
        var originalPros = review.Pros;

        review.ApplyEdit(rating: null, title: null, body: null, pros: null, cons: null, ReviewTestData.EditedAtUtc);

        Assert.Equal(originalRating, review.Rating);
        Assert.Equal(originalTitle, review.Title);
        Assert.Equal(originalBody, review.Body);
        Assert.Equal(originalPros, review.Pros);
        Assert.Null(review.Cons);
        Assert.Equal(ReviewTestData.EditedAtUtc, review.UpdatedAtUtc);
    }

    [Property]
    public void Pros_FollowTheThreeStateContract(Maybe<NonEmptyString>? prosChange)
    {
        var originalPros = "Original pro".ToNonEmpty();
        var review = ReviewTestData.CreateReview(pros: originalPros);

        review.ApplyEdit(rating: null, title: null, body: null, pros: prosChange, cons: null, ReviewTestData.EditedAtUtc);

        NonEmptyString? expected = prosChange is { } change ? change.Value : originalPros;
        Assert.Equal(expected, review.Pros);
    }

    [Property]
    public void Cons_CanBeSetFromNothing(Maybe<NonEmptyString>? consChange)
    {
        var review = ReviewTestData.CreateReview(cons: null);

        review.ApplyEdit(rating: null, title: null, body: null, pros: null, cons: consChange, ReviewTestData.EditedAtUtc);

        NonEmptyString? expected = consChange is { } change ? change.Value : null;
        Assert.Equal(expected, review.Cons);
    }

    [Property]
    public void OnlyProvidedFieldsChange(Rating? rating, NonEmptyString? title, NonEmptyString? body)
    {
        var review = ReviewTestData.CreateReview();
        var originalRating = review.Rating;
        var originalTitle = review.Title;
        var originalBody = review.Body;

        review.ApplyEdit(rating, title, body, pros: null, cons: null, ReviewTestData.EditedAtUtc);

        Assert.Equal(rating ?? originalRating, review.Rating);
        Assert.Equal(title ?? originalTitle, review.Title);
        Assert.Equal(body ?? originalBody, review.Body);
    }
}
