using System.Text.Json.Serialization;
using StrongTypes;

namespace ProductReviews.Api.Features.Reviews;

/// <summary>Every constrained field is a strong type — the OpenAPI schema carries the
/// constraints and there is no data annotation anywhere (ADR-0002).</summary>
public sealed record SubmitReviewRequest(
    Rating Rating,
    NonEmptyString Title,
    NonEmptyString Body,
    NonEmptyString? Pros,
    NonEmptyString? Cons);

/// <summary>PATCH semantics (ADR-0002, the Maybe&lt;T&gt; showcase): omitting a field leaves it
/// unchanged. For the clearable optionals, <c>{"value": null}</c> (or <c>{}</c>) clears and
/// <c>{"value": "…"}</c> replaces — three states one nullable cannot express.</summary>
public sealed record EditReviewRequest(
    Rating? Rating,
    NonEmptyString? Title,
    NonEmptyString? Body,
    Maybe<NonEmptyString>? Pros,
    Maybe<NonEmptyString>? Cons);

public sealed record ReviewResponse(
    Guid Id,
    Rating Rating,
    NonEmptyString Title,
    NonEmptyString Body,
    NonEmptyString? Pros,
    NonEmptyString? Cons,
    NonEmptyString AuthorName,
    int Score,
    bool Mine,
    bool? MyVote,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static ReviewResponse From(ReviewWithViewerContext reviewWithContext)
        => new(
            reviewWithContext.Review.Id,
            reviewWithContext.Review.Rating,
            reviewWithContext.Review.Title,
            reviewWithContext.Review.Body,
            reviewWithContext.Review.Pros,
            reviewWithContext.Review.Cons,
            reviewWithContext.Review.AuthorName,
            reviewWithContext.Review.Score,
            reviewWithContext.Mine,
            reviewWithContext.ViewerVote,
            reviewWithContext.Review.CreatedAtUtc,
            reviewWithContext.Review.UpdatedAtUtc);

    /// <summary>For submit/edit responses: the caller is the author, and authors can never vote on their own review.</summary>
    public static ReviewResponse FromOwn(Review review)
        => From(new ReviewWithViewerContext(review, Mine: true, ViewerVote: null));
}

public sealed record ReviewsPageResponse(
    IReadOnlyList<ReviewResponse> Items,
    NonNegative<int> TotalCount,
    Positive<int> Page,
    Positive<int> PageSize)
{
    public static ReviewsPageResponse From(ReviewsPage reviewsPage)
        => new(
            [.. reviewsPage.Reviews.Select(ReviewResponse.From)],
            reviewsPage.TotalCount,
            reviewsPage.Page,
            reviewsPage.PageSize);
}

/// <summary>The wire-facing sort enum: serialized as strings, mapped to the domain enum
/// with an exhaustive switch — domain enums never appear on the wire.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewSortOption
{
    MostHelpful,
    Newest,
    HighestRating,
    LowestRating,
}

public static class ReviewSortOptionExtensions
{
    public static ReviewSort Parse(this ReviewSortOption option) => option switch
    {
        ReviewSortOption.MostHelpful => ReviewSort.MostHelpful,
        ReviewSortOption.Newest => ReviewSort.Newest,
        ReviewSortOption.HighestRating => ReviewSort.HighestRating,
        ReviewSortOption.LowestRating => ReviewSort.LowestRating,
    };
}
