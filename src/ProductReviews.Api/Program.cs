using ProductReviews.Api.Infrastructure;
using StrongTypes.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

Observability.Configure(builder);
Health.Configure(builder);
Persistence.Configure(builder);
Authentication.Configure(builder);
RateLimits.Configure(builder);
ErrorHandling.Configure(builder);
OpenApi.Configure(builder);

builder.Services.AddControllers();
builder.Services.AddStrongTypes(options => options.JsonErrorKeyCasing = JsonErrorKeyCasing.CamelCase);
builder.Services.AddReviewsDomain();

var app = builder.Build();

ErrorHandling.Use(app);
Authentication.Use(app);
RateLimits.Use(app);
OpenApi.Use(app);
Health.Use(app);

app.MapControllers();

await Persistence.MigrateAndSeedAsync(app);

await app.RunAsync();

public partial class Program;
