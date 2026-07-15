using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProductReviews.Api.IntegrationTests;

[Collection(nameof(SharedApiCollection))]
public sealed class VotesApiTests(SharedApiContext context)
{
    private async Task<string> CreateReviewAsync(HttpClient author)
    {
        var created = await author.PostAsJsonAsync(
            "/api/products/logi-mx-master-3s/reviews",
            new { rating = 5, title = "Great mouse", body = "The scroll wheel alone is worth it." },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Vote_Flip_AndWithdraw_RecomputeTheScore()
    {
        using var author = context.CreateClientFor(SharedApiContext.RandomSubject(), "Author Abby");
        using var voter = context.CreateClientFor(SharedApiContext.RandomSubject(), "Voting Viktor");
        var reviewId = await CreateReviewAsync(author);

        var upvoted = await voter.PutAsJsonAsync(
            $"/api/reviews/{reviewId}/vote", new { isUpvote = true }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, upvoted.StatusCode);
        var afterUpvote = await upvoted.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(1, afterUpvote.GetProperty("score").GetInt32());
        Assert.True(afterUpvote.GetProperty("myVote").GetBoolean());

        var flipped = await voter.PutAsJsonAsync(
            $"/api/reviews/{reviewId}/vote", new { isUpvote = false }, TestContext.Current.CancellationToken);
        var afterFlip = await flipped.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(-1, afterFlip.GetProperty("score").GetInt32());
        Assert.False(afterFlip.GetProperty("myVote").GetBoolean());

        var withdrawn = await voter.DeleteAsync($"/api/reviews/{reviewId}/vote", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, withdrawn.StatusCode);
        var afterWithdrawal = await withdrawn.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(0, afterWithdrawal.GetProperty("score").GetInt32());
        Assert.Equal(JsonValueKind.Null, afterWithdrawal.GetProperty("myVote").ValueKind);
    }

    [Fact]
    public async Task Vote_OnYourOwnReview_IsRejected()
    {
        using var author = context.CreateClientFor(SharedApiContext.RandomSubject(), "Self-voting Sven");
        var reviewId = await CreateReviewAsync(author);

        var response = await author.PutAsJsonAsync(
            $"/api/reviews/{reviewId}/vote", new { isUpvote = true }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.True(problem.GetProperty("errors").TryGetProperty("id", out _));
    }

    [Fact]
    public async Task Vote_Anonymous_Returns401()
    {
        var response = await context.AnonymousClient.PutAsJsonAsync(
            $"/api/reviews/{Guid.NewGuid()}/vote", new { isUpvote = true }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Vote_OnUnknownReview_Returns404()
    {
        using var voter = context.CreateClientFor(SharedApiContext.RandomSubject(), "Voting Vera");

        var response = await voter.PutAsJsonAsync(
            $"/api/reviews/{Guid.NewGuid()}/vote", new { isUpvote = true }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
