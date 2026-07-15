using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProductReviews.Api.IntegrationTests;

/// <summary>The demo's headline claim: the OpenAPI document carries the strong-type
/// constraints, so the generated frontend client sees exactly what the API enforces.</summary>
[Collection(nameof(SharedApiCollection))]
public sealed class OpenApiDocumentTests(SharedApiContext context)
{
    private async Task<JsonElement> LoadDocumentAsync()
    {
        var response = await context.AnonymousClient.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NonEmptyString_CarriesMinLength_AndRequiredness()
    {
        var document = await LoadDocumentAsync();
        var submitRequest = document.GetProperty("components").GetProperty("schemas").GetProperty("SubmitReviewRequest");

        Assert.Equal(1, submitRequest.GetProperty("properties").GetProperty("title").GetProperty("minLength").GetInt32());
        Assert.Equal(1, submitRequest.GetProperty("properties").GetProperty("body").GetProperty("minLength").GetInt32());

        var required = submitRequest.GetProperty("required").EnumerateArray().Select(name => name.GetString()).ToList();
        Assert.Contains("rating", required);
        Assert.Contains("title", required);
        Assert.Contains("body", required);
        Assert.DoesNotContain("pros", required);
        Assert.True(submitRequest.GetProperty("properties").GetProperty("pros").GetProperty("nullable").GetBoolean());
    }

    [Fact]
    public async Task Email_CarriesTheEmailFormat()
    {
        var document = await LoadDocumentAsync();
        var email = document.GetProperty("components").GetProperty("schemas")
            .GetProperty("ProfileResponse").GetProperty("properties").GetProperty("email");

        Assert.Equal("email", email.GetProperty("format").GetString());
        Assert.Equal(254, email.GetProperty("maxLength").GetInt32());
    }

    [Fact]
    public async Task PositiveInt_CarriesExclusiveMinimum()
    {
        var document = await LoadDocumentAsync();
        var parameters = document.GetProperty("paths").GetProperty("/api/products/{slug}/reviews")
            .GetProperty("get").GetProperty("parameters");

        var pageParameter = parameters.EnumerateArray().Single(parameter => parameter.GetProperty("name").GetString() == "page");
        Assert.Equal(0, pageParameter.GetProperty("schema").GetProperty("minimum").GetInt32());
        Assert.True(pageParameter.GetProperty("schema").GetProperty("exclusiveMinimum").GetBoolean());
    }

    [Fact]
    public async Task CustomRatingWrapper_CarriesItsBounds()
    {
        var document = await LoadDocumentAsync();
        var rating = document.GetProperty("components").GetProperty("schemas")
            .GetProperty("SubmitReviewRequest").GetProperty("properties").GetProperty("rating");

        Assert.Equal(1, rating.GetProperty("minimum").GetInt32());
        Assert.Equal(5, rating.GetProperty("maximum").GetInt32());
    }
}
