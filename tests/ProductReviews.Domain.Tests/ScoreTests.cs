using FsCheck.Xunit;
using ProductReviews.Domain.Votes;
using Xunit;

namespace ProductReviews.Domain.Tests;

public sealed class ScoreTests
{
    [Property]
    public bool RefreshScore_IsAlwaysUpvotesMinusDownvotes(bool[] voteDirections)
    {
        var review = ReviewTestData.CreateReview();
        var votes = voteDirections
            .Select(isUpvote => new ReviewVote(review.Id, Guid.NewGuid(), isUpvote, ReviewTestData.CreatedAtUtc))
            .ToList();

        review.RefreshScore(votes);

        return review.Score == votes.Count(vote => vote.IsUpvote) - votes.Count(vote => !vote.IsUpvote);
    }

    [Fact]
    public void ChangeDirection_FlipsTheContribution()
    {
        var vote = new ReviewVote(Guid.NewGuid(), Guid.NewGuid(), isUpvote: true, ReviewTestData.CreatedAtUtc);
        Assert.Equal(1, vote.ScoreContribution);

        vote.ChangeDirection(isUpvote: false);
        Assert.Equal(-1, vote.ScoreContribution);
    }
}
