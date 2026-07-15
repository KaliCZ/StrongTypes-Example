using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProductReviews.Api.IntegrationTests;

[Collection(nameof(SharedApiCollection))]
public sealed class ProfileApiTests(SharedApiContext context)
{
    [Fact]
    public async Task GetMe_Anonymous_Returns401()
    {
        var response = await context.AnonymousClient.GetAsync("/api/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ReturnsTheProfileFromTheToken()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "Profile Pat", "pat@example.com");

        var response = await client.GetAsync("/api/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Profile Pat", profile.GetProperty("displayName").GetString());
        Assert.Equal("pat@example.com", profile.GetProperty("email").GetString());
        Assert.NotEqual(Guid.Empty, profile.GetProperty("authorId").GetGuid());
    }

    [Fact]
    public async Task GetMe_MalformedEmailClaim_YieldsNullEmail_NotAnError()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "No-mail Ned", "not-an-email");

        var response = await client.GetAsync("/api/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(JsonValueKind.Null, profile.GetProperty("email").ValueKind);
    }
}
