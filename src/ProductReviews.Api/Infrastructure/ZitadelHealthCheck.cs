using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProductReviews.Api.Infrastructure;

/// <summary>Probes the OIDC discovery document so a dead or misconfigured Zitadel surfaces
/// in /health rather than as sign-in failures. Degraded, not Unhealthy: reads keep working
/// without the identity provider — only sign-in and writes are affected.</summary>
internal sealed class ZitadelHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public const string ClientName = "zitadel-health";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var authority = configuration["Oidc:Authority"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(authority))
        {
            return HealthCheckResult.Degraded("Oidc:Authority is not configured; sign-in is unavailable.");
        }

        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            using var response = await client.GetAsync($"{authority}/.well-known/openid-configuration", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"OIDC discovery returned {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Degraded("OIDC authority unreachable.", exception);
        }
    }
}
