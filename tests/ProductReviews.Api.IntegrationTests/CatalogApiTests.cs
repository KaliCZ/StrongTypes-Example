using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProductReviews.Api.IntegrationTests;

// Wire-level assertions on anonymous JSON (never the API's DTO classes), so a
// contract rename fails a test instead of silently tracking.
[Collection(nameof(SharedApiCollection))]
public sealed class CatalogApiTests(SharedApiContext context)
{
    [Fact]
    public async Task GetCatalog_ReturnsTheSeededProducts_WithTheExpectedContract()
    {
        var response = await context.AnonymousClient.GetAsync("/api/products", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(10, products.GetArrayLength());

        // No test writes reviews on the first seeded product, so its aggregates are stable.
        var first = products[0];
        Assert.Equal(1, first.GetProperty("id").GetInt64());
        Assert.Equal("sony-wh-1000xm5", first.GetProperty("slug").GetString());
        Assert.Equal("Sony WH-1000XM5 Wireless Headphones", first.GetProperty("name").GetString());
        Assert.Equal(4, first.GetProperty("reviewCount").GetInt32());
        Assert.Equal(4.5, first.GetProperty("averageRating").GetDouble());
        Assert.False(string.IsNullOrEmpty(first.GetProperty("imageUrl").GetString()));
    }

    [Fact]
    public async Task GetProduct_UnknownSlug_Returns404Problem()
    {
        var response = await context.AnonymousClient.GetAsync("/api/products/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_ForAnonymousViewer_HasNoOwnReview()
    {
        var response = await context.AnonymousClient.GetAsync("/api/products/ipad-air-11", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("ipad-air-11", product.GetProperty("slug").GetString());
        Assert.Equal(JsonValueKind.Null, product.GetProperty("myReviewId").ValueKind);
        Assert.False(string.IsNullOrEmpty(product.GetProperty("description").GetString()));
    }
}
