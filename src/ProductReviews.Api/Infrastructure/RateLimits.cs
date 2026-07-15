using System.Security.Claims;
using System.Threading.RateLimiting;

namespace ProductReviews.Api.Infrastructure;

/// <summary>A single fixed-window limiter on write endpoints, partitioned per user
/// (falling back to the caller's IP for the odd unauthenticated hit). Reads are unlimited.</summary>
public static class RateLimits
{
    public const string WritePolicy = "write";

    private const int WritesPerWindow = 30;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(WritePolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.User.FindFirstValue("sub")
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = WritesPerWindow,
                    Window = Window,
                    QueueLimit = 0,
                }));
        });
    }

    public static void Use(WebApplication app) => app.UseRateLimiter();
}
