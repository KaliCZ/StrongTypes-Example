using ProductReviews.Domain.Votes;

namespace ProductReviews.Api.Features.Votes;

public sealed record CastVoteRequest(bool IsUpvote);

public sealed record VoteResponse(int Score, bool? MyVote)
{
    public static VoteResponse From(VoteSummary voteSummary)
        => new(voteSummary.Score, voteSummary.ViewerVote);
}
