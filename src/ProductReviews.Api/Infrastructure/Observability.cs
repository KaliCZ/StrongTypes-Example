using ProductReviews.Domain.Persistence;

namespace ProductReviews.Api.Infrastructure;

/// <summary>API-specific health checks. Tracing, metrics, and the /health endpoints
/// themselves come from ServiceDefaults.</summary>
public static class Observability
{
    public static void Configure(WebApplicationBuilder builder)
        => builder.Services.AddHealthChecks().AddDbContextCheck<ReviewsDbContext>("database");
}
