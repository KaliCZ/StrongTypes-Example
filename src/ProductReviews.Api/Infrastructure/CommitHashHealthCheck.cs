using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProductReviews.Api.Infrastructure;

internal sealed class CommitHashHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, data: new Dictionary<string, object>
        {
            ["commit"] = AppVersion.CommitHash,
        }));
}
