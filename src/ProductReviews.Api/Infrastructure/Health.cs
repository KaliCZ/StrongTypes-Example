using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductReviews.Api.Persistence;

namespace ProductReviews.Api.Infrastructure;

/// <summary>/alive = process up + which build, no dependencies — a shared Postgres or
/// Zitadel outage must never read as a dead process. /health = full readiness, every
/// dependency included. Both bodies carry the running commit for a deploy gate to grep.</summary>
public static class Health
{
    public const string HealthEndpointPath = "/health";
    public const string AlivenessEndpointPath = "/alive";

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient(ZitadelHealthCheck.ClientName, client => client.Timeout = TimeSpan.FromSeconds(5));

        builder.Services.AddHealthChecks()
            .AddCheck<CommitHashHealthCheck>("version", tags: ["live"])
            .AddDbContextCheck<ReviewsDbContext>("database")
            .AddCheck<ZitadelHealthCheck>("zitadel");
    }

    public static void Use(WebApplication app)
    {
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = WriteStatusAndCommitAsync,
        });

        app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions
        {
            ResponseWriter = WriteStatusAndCommitAsync,
        });
    }

    // "<status> <commit>" — greppable by a deploy gate, no per-check detail to leak.
    private static Task WriteStatusAndCommitAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync($"{report.Status} {AppVersion.CommitHash}", context.RequestAborted);
    }
}
