using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProductReviews.Api.IntegrationTests;

[Collection(nameof(SharedApiCollection))]
public sealed class ReviewsQueryApiTests(SharedApiContext context)
{
    [Fact]
    public async Task GetReviews_FilteredByRating_ReturnsOnlyMatchingReviews()
    {
        var response = await context.AnonymousClient.GetAsync(
            "/api/products/acme-smartwatch/reviews?ratings=1&ratings=2", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(3, page.GetProperty("totalCount").GetInt32());
        foreach (var review in page.GetProperty("items").EnumerateArray())
        {
            Assert.InRange(review.GetProperty("rating").GetInt32(), 1, 2);
        }
    }

    [Fact]
    public async Task GetReviews_SortedByNewest_IsInDescendingCreationOrder()
    {
        var response = await context.AnonymousClient.GetAsync(
            "/api/products/sony-wh-1000xm5/reviews?sort=newest", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var createdTimestamps = page.GetProperty("items").EnumerateArray()
            .Select(review => review.GetProperty("createdAtUtc").GetDateTime())
            .ToList();
        Assert.True(createdTimestamps.Count >= 2);
        Assert.Equal(createdTimestamps.OrderByDescending(timestamp => timestamp), createdTimestamps);
    }

    [Fact]
    public async Task GetReviews_SortedByMostHelpful_IsInDescendingScoreOrder()
    {
        var response = await context.AnonymousClient.GetAsync(
            "/api/products/sony-wh-1000xm5/reviews?sort=mostHelpful", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var scores = page.GetProperty("items").EnumerateArray()
            .Select(review => review.GetProperty("score").GetInt32())
            .ToList();
        Assert.Equal(scores.OrderByDescending(score => score), scores);
    }

    [Fact]
    public async Task GetReviews_PageSizeIsCappedAtFifty()
    {
        var response = await context.AnonymousClient.GetAsync(
            "/api/products/sony-wh-1000xm5/reviews?pageSize=5000", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(50, page.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task GetReviews_ZeroPage_IsRejectedByThePositiveWrapper()
    {
        var response = await context.AnonymousClient.GetAsync(
            "/api/products/sony-wh-1000xm5/reviews?page=0", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
