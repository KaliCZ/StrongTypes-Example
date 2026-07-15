using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ProductReviews.Api.Infrastructure;

/// <summary>JwtBearer against the OIDC authority (Zitadel locally — the AppHost injects
/// Oidc__Authority and Oidc__Audience). Authorization is applied per endpoint with
/// [Authorize]; there is deliberately no blanket RequireAuthorization anywhere.</summary>
public static class Authentication
{
    public static void Configure(WebApplicationBuilder builder)
    {
        var authority = builder.Configuration["Oidc:Authority"]?.TrimEnd('/');
        var audience = builder.Configuration["Oidc:Audience"];

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
            });

        builder.Services.AddAuthorization();
    }

    public static void Use(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
