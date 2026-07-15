using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ProductReviews.Domain.Reviews;
using StrongTypes;

namespace ProductReviews.Api.Infrastructure;

/// <summary>Maps the authenticated principal to the domain's <see cref="ReviewAuthor"/>.
/// AuthorId is the SHA-256 of the OIDC <c>sub</c> claim (first 16 bytes as a Guid), so the
/// database stays Guid-keyed whatever shape the identity provider's ids have (ADR-0005).</summary>
public static class CurrentUser
{
    public static ReviewAuthor Author(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated principal is missing the 'sub' claim.");
        var displayName = user.FindFirstValue("name") ?? subject;
        return new ReviewAuthor(SubjectToGuid(subject), displayName.ToNonEmpty());
    }

    public static Guid? AuthorIdOrNull(this ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true && user.FindFirstValue("sub") is { } subject
            ? SubjectToGuid(subject)
            : null;

    private static Guid SubjectToGuid(string subject)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(subject));
        return new Guid(hash.AsSpan(0, 16));
    }
}
