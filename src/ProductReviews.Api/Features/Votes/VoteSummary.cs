namespace ProductReviews.Api.Features.Votes;

/// <summary>The state a voter sees after a vote operation: the review's recomputed
/// score and their own current vote (null = no vote).</summary>
public sealed record VoteSummary(int Score, bool? ViewerVote);
