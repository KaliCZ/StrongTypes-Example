using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductReviews.Api.Infrastructure;
using StrongTypes;

namespace ProductReviews.Api.Features.Profile;

public sealed record ProfileResponse(NonEmptyString DisplayName, Email? Email, Guid AuthorId);

[ApiController]
[Route("api/me")]
public sealed class ProfileController : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ProfileResponse> GetProfile()
    {
        var author = User.Author();
        // TryCreate (never Create): the claim is external input — absent or malformed simply means no email.
        var email = Email.TryCreate(User.FindFirstValue("email"));
        return Ok(new ProfileResponse(author.DisplayName, email, author.AuthorId));
    }
}
