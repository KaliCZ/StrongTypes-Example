using System.Net;
using Xunit;

namespace ProductReviews.Api.IntegrationTests;

[Collection(nameof(SharedApiCollection))]
public sealed class HealthApiTests(SharedApiContext context)
{
    [Fact]
    public async Task Alive_ReportsHealthyAndTheRunningCommit_WithoutTouchingDependencies()
    {
        var response = await context.AnonymousClient.GetAsync("/alive", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        // "Healthy <commit>": liveness carries the build identity but no dependency state —
        // Zitadel is not configured here, yet the process still reports alive.
        Assert.StartsWith("Healthy ", body);
        Assert.False(string.IsNullOrWhiteSpace(body["Healthy ".Length..]));
    }

    [Fact]
    public async Task Health_ReportsDegradedWhenTheIdentityProviderIsMissing_ButTheDatabaseCheckPasses()
    {
        var response = await context.AnonymousClient.GetAsync("/health", TestContext.Current.CancellationToken);

        // Degraded (not Unhealthy): the database is reachable and reads keep working; only
        // sign-in is unavailable without an OIDC authority. Degraded still returns 200.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.StartsWith("Degraded ", body);
    }
}
