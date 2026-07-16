using ProductReviews.Api.Features.Reviews;

namespace ProductReviews.Api.Features.Votes;

/// <summary>Proof-of-loading aggregate: a review together with all its votes, so the
/// helpfulness score can be recomputed without trusting a lazily-loaded navigation.
/// Construct via <c>CompleteQueries</c>, which owns the matching Include chain.</summary>
public sealed record ReviewWithVotes(Review Review, IReadOnlyList<ReviewVote> Votes)
{
    public static ReviewWithVotes FromCompleteQuery(Review review)
        => new(review, [.. review.Votes]);

    public void RefreshScore()
        => Review.RefreshScore([.. Votes]);
}
