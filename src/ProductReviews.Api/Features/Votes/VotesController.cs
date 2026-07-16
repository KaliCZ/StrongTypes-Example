using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductReviews.Api.Infrastructure;
using StrongTypes;

namespace ProductReviews.Api.Features.Votes;

[ApiController]
[Route("api/reviews/{id:guid}/vote")]
public sealed class VotesController(
    CastVoteHandler castVote,
    RemoveVoteHandler removeVote) : ControllerBase
{
    [HttpPut]
    [Authorize]
    [EnableRateLimiting(RateLimits.WritePolicy)]
    [ProducesResponseType<VoteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoteResponse>> CastVote(
        Guid id,
        CastVoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await castVote.HandleAsync(id, User.Author().AuthorId, request.IsUpvote, cancellationToken);

        if (result.Error is { } error)
        {
            return error switch
            {
                CastVoteError.ReviewNotFound => NotFound(),
                CastVoteError.OwnReview => OwnReviewProblem(),
            };
        }

        return Ok(VoteResponse.From(result.Success!));
    }

    [HttpDelete]
    [Authorize]
    [EnableRateLimiting(RateLimits.WritePolicy)]
    [ProducesResponseType<VoteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoteResponse>> RemoveVote(Guid id, CancellationToken cancellationToken)
    {
        var result = await removeVote.HandleAsync(id, User.Author().AuthorId, cancellationToken);

        if (result.Error is { } error)
        {
            return error switch
            {
                RemoveVoteError.ReviewNotFound => NotFound(),
            };
        }

        return Ok(VoteResponse.From(result.Success!));
    }

    private ActionResult OwnReviewProblem()
    {
        ModelState.AddModelError("id", "You cannot vote on your own review.");
        return ValidationProblem(ModelState);
    }
}
