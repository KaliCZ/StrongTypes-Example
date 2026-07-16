using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ProductReviews.Api.IntegrationTests;

/// <summary>Mints JWTs against a test-owned symmetric key. The API's real JwtBearer
/// pipeline validates them (ADR-0005) — only the trust anchor is swapped, not the code path.</summary>
public static class TestTokens
{
    public const string Issuer = "https://tests.productreviews.local";
    public const string Audience = "productreviews-tests";

    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("productreviews-integration-tests-signing-key-0123456789"));

    private static readonly JsonWebTokenHandler Handler = new();

    public static string CreateToken(string subject, string displayName, string? email = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = subject,
            ["name"] = displayName,
        };
        if (email is not null)
        {
            claims["email"] = email;
        }

        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        });
    }
}
