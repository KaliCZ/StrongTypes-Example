using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Xunit;

namespace ProductReviews.Api.IntegrationTests;

public sealed class ReviewsApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development runs the real migrate + seed path at startup (ADR-0004) —
        // the tests exercise it instead of preparing the schema themselves.
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:productreviews", connectionString);

        builder.ConfigureServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = TestTokens.Issuer,
                    ValidAudience = TestTokens.Audience,
                    IssuerSigningKey = TestTokens.SigningKey,
                    NameClaimType = "name",
                };
            }));
    }
}

/// <summary>One Postgres container + one API host for the whole run (ADR-0005);
/// tests isolate through unique authors/products, never by resetting the database.</summary>
public sealed class SharedApiContext : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17.5").Build();

    public ReviewsApiFactory Factory { get; private set; } = null!;

    public HttpClient AnonymousClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        Factory = new ReviewsApiFactory(postgres.GetConnectionString());
        AnonymousClient = Factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        AnonymousClient.Dispose();
        await Factory.DisposeAsync();
        await postgres.DisposeAsync();
    }

    /// <summary>A client authenticated as the given subject. Tests mint a fresh random
    /// subject per scenario, which is what keeps them independent (one review per
    /// product per author).</summary>
    public HttpClient CreateClientFor(string subject, string displayName, string? email = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.CreateToken(subject, displayName, email));
        return client;
    }

    public static string RandomSubject() => $"test-user-{Guid.NewGuid():N}";
}

[CollectionDefinition(nameof(SharedApiCollection))]
public sealed class SharedApiCollection : ICollectionFixture<SharedApiContext>;
