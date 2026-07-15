namespace ProductReviews.Domain.Votes;

/// <summary>One reviewer's helpful/not-helpful verdict on one review.
/// The composite key (ReviewId, VoterId) makes "one vote per reviewer per review" structural.</summary>
public sealed class ReviewVote
{
    private ReviewVote()
    {
    }

    public ReviewVote(Guid reviewId, Guid voterId, bool isUpvote, DateTime createdAtUtc)
    {
        ReviewId = reviewId;
        VoterId = voterId;
        IsUpvote = isUpvote;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ReviewId { get; private set; }

    public Guid VoterId { get; private set; }

    public bool IsUpvote { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public int ScoreContribution => IsUpvote ? 1 : -1;

    public void ChangeDirection(bool isUpvote) => IsUpvote = isUpvote;
}
