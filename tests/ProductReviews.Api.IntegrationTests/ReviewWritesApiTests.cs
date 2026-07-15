using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProductReviews.Api.IntegrationTests;

[Collection(nameof(SharedApiCollection))]
public sealed class ReviewWritesApiTests(SharedApiContext context)
{
    private static object ValidReviewBody(int rating = 5) => new
    {
        rating,
        title = "Exceeded my expectations",
        body = "Bought it on a whim and it turned out to be the best purchase this year.",
        pros = "Sturdy build",
        cons = (string?)null,
    };

    [Fact]
    public async Task SubmitReview_Anonymous_Returns401()
    {
        var response = await context.AnonymousClient.PostAsJsonAsync(
            "/api/products/usb-c-cable-pack/reviews", ValidReviewBody(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_UpdatesTheProductAggregates()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "Paula Prospect");

        // powerjuice-10000 is seeded with ratings 2, 3, 1 → count 3, average 2.0. This is the
        // only test that writes to it.
        var created = await client.PostAsJsonAsync(
            "/api/products/powerjuice-10000/reviews", ValidReviewBody(rating: 5), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var review = await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(5, review.GetProperty("rating").GetInt32());
        Assert.True(review.GetProperty("mine").GetBoolean());
        Assert.Equal("Paula Prospect", review.GetProperty("authorName").GetString());
        Assert.Equal(JsonValueKind.Null, review.GetProperty("updatedAtUtc").ValueKind);

        var productResponse = await client.GetAsync("/api/products/powerjuice-10000", TestContext.Current.CancellationToken);
        var product = await productResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(4, product.GetProperty("reviewCount").GetInt32());
        Assert.Equal(2.75, product.GetProperty("averageRating").GetDouble());
        Assert.Equal(
            review.GetProperty("id").GetString(),
            product.GetProperty("myReviewId").GetString());
    }

    [Fact]
    public async Task SubmitReview_EmptyTitle_FailsAtTheJsonBoundary_WithTheFieldKey()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "Blank Betty");

        var response = await client.PostAsJsonAsync(
            "/api/products/usb-c-cable-pack/reviews",
            new { rating = 4, title = "", body = "Fine." },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        // Key is normalized to the camelCase property name by Kalicz.StrongTypes.AspNetCore.
        Assert.True(problem.GetProperty("errors").TryGetProperty("title", out _));
    }

    [Fact]
    public async Task SubmitReview_OutOfRangeRating_FailsAtTheJsonBoundary()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "Sixstar Sam");

        var response = await client.PostAsJsonAsync(
            "/api/products/usb-c-cable-pack/reviews",
            new { rating = 6, title = "All the stars", body = "More stars than the scale allows." },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.True(problem.GetProperty("errors").TryGetProperty("rating", out _));
    }

    [Fact]
    public async Task SubmitReview_SecondReviewForTheSameProduct_Returns409()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "Repeat Rita");

        var first = await client.PostAsJsonAsync(
            "/api/products/single-origin-coffee/reviews", ValidReviewBody(), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync(
            "/api/products/single-origin-coffee/reviews", ValidReviewBody(rating: 2), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_UnknownProduct_Returns404()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "Lost Lars");

        var response = await client.PostAsJsonAsync(
            "/api/products/not-a-product/reviews", ValidReviewBody(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditReview_FollowsTheThreeStateMaybeContract()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "Editing Edith");

        var created = await client.PostAsJsonAsync(
            "/api/products/travelpro-tripod/reviews",
            new { rating = 4, title = "Good tripod", body = "Steady enough.", pros = "Light", cons = "Wobbly head" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var reviewId = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetString();

        // {} = Maybe.None = clear pros; cons omitted = leave unchanged.
        var cleared = await client.PatchAsJsonAsync(
            $"/api/reviews/{reviewId}", new { pros = new { } }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var afterClear = await cleared.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(JsonValueKind.Null, afterClear.GetProperty("pros").ValueKind);
        Assert.Equal("Wobbly head", afterClear.GetProperty("cons").GetString());

        // {"Value": …} = Maybe.Some = set; plain nullable fields update in place.
        var updated = await client.PatchAsJsonAsync(
            $"/api/reviews/{reviewId}",
            new { rating = 2, pros = new { Value = "Still light" } },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var afterUpdate = await updated.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(2, afterUpdate.GetProperty("rating").GetInt32());
        Assert.Equal("Still light", afterUpdate.GetProperty("pros").GetString());
        Assert.Equal("Good tripod", afterUpdate.GetProperty("title").GetString());
        Assert.NotEqual(JsonValueKind.Null, afterUpdate.GetProperty("updatedAtUtc").ValueKind);
    }

    [Fact]
    public async Task EditReview_ByAnotherUser_Returns403()
    {
        using var author = context.CreateClientFor(SharedApiContext.RandomSubject(), "Owner Olga");
        using var intruder = context.CreateClientFor(SharedApiContext.RandomSubject(), "Intruding Ivan");

        var created = await author.PostAsJsonAsync(
            "/api/products/xyz-mechanical-keyboard/reviews", ValidReviewBody(), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var reviewId = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetString();

        var response = await intruder.PatchAsJsonAsync(
            $"/api/reviews/{reviewId}", new { title = "Hijacked" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteReview_FreesTheSlotForANewReview()
    {
        using var client = context.CreateClientFor(SharedApiContext.RandomSubject(), "Deleting Dana");

        var created = await client.PostAsJsonAsync(
            "/api/products/boombox-mini/reviews", ValidReviewBody(rating: 1), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var reviewId = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken))
            .GetProperty("id").GetString();

        var deleted = await client.DeleteAsync($"/api/reviews/{reviewId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var resubmitted = await client.PostAsJsonAsync(
            "/api/products/boombox-mini/reviews", ValidReviewBody(rating: 3), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, resubmitted.StatusCode);
    }
}
