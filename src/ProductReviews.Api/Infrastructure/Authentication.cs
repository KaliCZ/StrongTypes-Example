using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace ProductReviews.Api.Infrastructure;

/// <summary>JwtBearer against the OIDC authority (Zitadel locally — the AppHost injects
/// Oidc__Authority and Oidc__Audience). Authorization is applied per endpoint with
/// [Authorize]; there is deliberately no blanket RequireAuthorization anywhere.</summary>
public static class Authentication
{
    private const string UserinfoClientName = "oidc-userinfo";

    public static void Configure(WebApplicationBuilder builder)
    {
        var authority = builder.Configuration["Oidc:Authority"]?.TrimEnd('/');
        var audience = builder.Configuration["Oidc:Audience"];

        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient(UserinfoClientName);

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = authority?.StartsWith("https", StringComparison.OrdinalIgnoreCase) ?? true;
                options.MapInboundClaims = false;
                options.RefreshOnIssuerKeyNotFound = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                };
                if (authority is not null)
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context => EnrichFromUserinfoAsync(context, authority),
                    };
                }
            });

        builder.Services.AddAuthorization();
    }

    public static void Use(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    // Zitadel's JWT access tokens carry only the subject; the display name and email live
    // at the userinfo endpoint. Fetched once per subject (cached), so the domain snapshots
    // AuthorName from the identity provider — never from anything the browser sends.
    private static async Task EnrichFromUserinfoAsync(TokenValidatedContext context, string authority)
    {
        if (context.Principal?.FindFirst("name") is not null)
        {
            return;
        }
        var subject = context.Principal?.FindFirst("sub")?.Value;
        var authorizationHeader = context.HttpContext.Request.Headers.Authorization.ToString();
        if (subject is null || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        var profile = await cache.GetOrCreateAsync($"oidc-userinfo:{subject}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient(UserinfoClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{authority}/oidc/v1/userinfo");
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
            using var response = await client.SendAsync(request, context.HttpContext.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(context.HttpContext.RequestAborted));
            return new UserinfoProfile(
                document.RootElement.TryGetProperty("name", out var name) ? name.GetString() : null,
                document.RootElement.TryGetProperty("email", out var email) ? email.GetString() : null);
        });
        if (profile is null)
        {
            return;
        }

        var claims = new List<Claim>();
        if (profile.DisplayName is not null)
        {
            claims.Add(new Claim("name", profile.DisplayName));
        }
        if (profile.Email is not null && context.Principal!.FindFirst("email") is null)
        {
            claims.Add(new Claim("email", profile.Email));
        }
        if (claims.Count > 0)
        {
            context.Principal!.AddIdentity(new ClaimsIdentity(claims));
        }
    }

    private sealed record UserinfoProfile(string? DisplayName, string? Email);
}
