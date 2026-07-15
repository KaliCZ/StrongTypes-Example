using ProductReviews.Api.Infrastructure;
using ProductReviews.Domain;
using ProductReviews.ServiceDefaults;
using StrongTypes.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

Persistence.Configure(builder);
Authentication.Configure(builder);
RateLimits.Configure(builder);
ErrorHandling.Configure(builder);
OpenApi.Configure(builder);
Observability.Configure(builder);

builder.Services.AddControllers();
builder.Services.AddStrongTypes(options => options.JsonErrorKeyCasing = JsonErrorKeyCasing.CamelCase);
builder.Services.AddReviewsDomain();

var app = builder.Build();

ErrorHandling.Use(app);
Authentication.Use(app);
RateLimits.Use(app);
OpenApi.Use(app);

app.MapDefaultEndpoints();
app.MapControllers();

await Persistence.MigrateAndSeedAsync(app);

await app.RunAsync();

public partial class Program;
