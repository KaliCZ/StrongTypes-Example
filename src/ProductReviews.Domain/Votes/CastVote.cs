using ProductReviews.Domain.Persistence;
using StrongTypes;

namespace ProductReviews.Domain.Votes;

public enum CastVoteError
{
    ReviewNotFound,
    OwnReview,
}

public sealed class CastVoteHandler(ReviewsDbContext dbContext, TimeProvider timeProvider)
{
    /// <summary>Idempotent upsert: a first vote inserts, a repeated vote re-points the existing one.</summary>
    public async Task<Result<VoteSummary, CastVoteError>> HandleAsync(
        Guid reviewId,
        Guid voterId,
        bool isUpvote,
        CancellationToken cancellationToken)
    {
        var review = await dbContext.CompleteReviewById(reviewId, cancellationToken);
        if (review is null)
        {
            return CastVoteError.ReviewNotFound;
        }
        if (review.Review.AuthorId == voterId)
        {
            return CastVoteError.OwnReview;
        }

        var existingVote = review.Votes.SingleOrDefault(vote => vote.VoterId == voterId);
        ReviewWithVotes afterVote;
        if (existingVote is null)
        {
            var vote = new ReviewVote(reviewId, voterId, isUpvote, timeProvider.GetUtcNow().UtcDateTime);
            dbContext.ReviewVotes.Add(vote);
            afterVote = review with { Votes = [.. review.Votes, vote] };
        }
        else
        {
            existingVote.ChangeDirection(isUpvote);
            afterVote = review;
        }

        afterVote.RefreshScore();
        await dbContext.SaveChangesAsync(cancellationToken);
        return new VoteSummary(afterVote.Review.Score, isUpvote);
    }
}
