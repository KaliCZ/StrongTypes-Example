using ProductReviews.Api.Persistence;
using StrongTypes;

namespace ProductReviews.Api.Features.Votes;

public enum RemoveVoteError
{
    ReviewNotFound,
}

public sealed class RemoveVoteHandler(ReviewsDbContext dbContext)
{
    /// <summary>Withdrawing a vote that was never cast is a success — the end state is identical.</summary>
    public async Task<Result<VoteSummary, RemoveVoteError>> HandleAsync(
        Guid reviewId,
        Guid voterId,
        CancellationToken cancellationToken)
    {
        var review = await dbContext.CompleteReviewById(reviewId, cancellationToken);
        if (review is null)
        {
            return RemoveVoteError.ReviewNotFound;
        }

        var existingVote = review.Votes.SingleOrDefault(vote => vote.VoterId == voterId);
        if (existingVote is null)
        {
            return new VoteSummary(review.Review.Score, null);
        }

        dbContext.ReviewVotes.Remove(existingVote);
        var afterRemoval = review with { Votes = [.. review.Votes.Where(vote => vote.VoterId != voterId)] };
        afterRemoval.RefreshScore();

        await dbContext.SaveChangesAsync(cancellationToken);
        return new VoteSummary(afterRemoval.Review.Score, null);
    }
}
