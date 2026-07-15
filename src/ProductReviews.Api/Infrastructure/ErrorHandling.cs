namespace ProductReviews.Api.Infrastructure;

/// <summary>Unhandled exceptions become RFC 7807 responses. Business failures never get
/// here — they travel as Result error enums and are mapped by controllers (ADR-0003).</summary>
public static class ErrorHandling
{
    public static void Configure(WebApplicationBuilder builder)
        => builder.Services.AddProblemDetails();

    public static void Use(WebApplication app)
        => app.UseExceptionHandler();
}
