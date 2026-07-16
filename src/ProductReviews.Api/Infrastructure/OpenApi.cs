using Microsoft.OpenApi;
using ProductReviews.Api.Features.Reviews;
using StrongTypes.OpenApi.Swashbuckle;

namespace ProductReviews.Api.Infrastructure;

/// <summary>Swashbuckle with AddStrongTypes(), so the document carries the real
/// constraints (email format, minLength, exclusiveMinimum). The generated TypeScript
/// client is built from this document — it is the frontend contract (ADR-0004).</summary>
public static class OpenApi
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddStrongTypes();

            // Our own Rating wrapper is unknown to the package, so its bounds are declared here —
            // the one place the 1..5 rule exists outside the type itself (schema, not validation).
            options.MapType<Rating>(() => RatingSchema());
            options.MapType<Rating?>(() => RatingSchema());

            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
        });
    }

    public static void Use(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.UseSwagger();
        app.UseSwaggerUI();
    }

    private static OpenApiSchema RatingSchema() => new()
    {
        Type = JsonSchemaType.Integer,
        Format = "int32",
        Minimum = Rating.MinimumStars.ToString(),
        Maximum = Rating.MaximumStars.ToString(),
    };
}
