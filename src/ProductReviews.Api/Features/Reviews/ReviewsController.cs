using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductReviews.Api.Infrastructure;
using ProductReviews.Domain.Reviews;
using StrongTypes;

namespace ProductReviews.Api.Features.Reviews;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(
    GetReviewsPageHandler getReviewsPage,
    SubmitReviewHandler submitReview,
    EditReviewHandler editReview,
    DeleteReviewHandler deleteReview) : ControllerBase
{
    [HttpGet("/api/products/{slug}/reviews")]
    [AllowAnonymous]
    [ProducesResponseType<ReviewsPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewsPageResponse>> GetReviews(
        NonEmptyString slug,
        [FromQuery] ReviewSortOption sort = ReviewSortOption.MostHelpful,
        [FromQuery] Rating[]? ratings = null,
        [FromQuery] Positive<int>? page = null,
        [FromQuery] Positive<int>? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var reviewsPage = await getReviewsPage.HandleAsync(
            slug,
            sort.Parse(),
            ratings ?? [],
            page ?? 1.ToPositive(),
            pageSize ?? 10.ToPositive(),
            User.AuthorIdOrNull(),
            cancellationToken);
        return reviewsPage is null ? NotFound() : Ok(ReviewsPageResponse.From(reviewsPage));
    }

    [HttpPost("/api/products/{slug}/reviews")]
    [Authorize]
    [EnableRateLimiting(RateLimits.WritePolicy)]
    [ProducesResponseType<ReviewResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewResponse>> SubmitReview(
        NonEmptyString slug,
        SubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await submitReview.HandleAsync(
            slug, User.Author(), request.Rating, request.Title, request.Body, request.Pros, request.Cons, cancellationToken);

        if (result.Error is { } error)
        {
            return error switch
            {
                SubmitReviewError.ProductNotFound => NotFound(),
                SubmitReviewError.AlreadyReviewed => Conflict(new ProblemDetails
                {
                    Title = "AlreadyReviewed",
                    Detail = "You have already reviewed this product. Edit or delete your existing review instead.",
                    Status = StatusCodes.Status409Conflict,
                }),
            };
        }

        var response = ReviewResponse.FromOwn(result.Success!);
        return Created($"/api/products/{slug}/reviews", response);
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    [EnableRateLimiting(RateLimits.WritePolicy)]
    [ProducesResponseType<ReviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewResponse>> EditReview(
        Guid id,
        EditReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await editReview.HandleAsync(
            id, User.Author().AuthorId, request.Rating, request.Title, request.Body, request.Pros, request.Cons, cancellationToken);

        if (result.Error is { } error)
        {
            return error switch
            {
                EditReviewError.ReviewNotFound => NotFound(),
                EditReviewError.NotYourReview => Forbid(),
            };
        }

        return Ok(ReviewResponse.FromOwn(result.Success!));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [EnableRateLimiting(RateLimits.WritePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var error = await deleteReview.HandleAsync(id, User.Author().AuthorId, cancellationToken);
        if (error is not { } deleteError)
        {
            return NoContent();
        }

        return deleteError switch
        {
            DeleteReviewError.ReviewNotFound => NotFound(),
            DeleteReviewError.NotYourReview => Forbid(),
        };
    }
}
