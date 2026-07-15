using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProductReviews.AppHost.Zitadel;

/// <summary>Carries the OIDC client id from provisioning (which knows the frontend's origin)
/// to the api and frontend resources — both await it in their env callbacks.</summary>
public sealed class ZitadelOidc
{
    public TaskCompletionSource<string> ClientId { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>Idempotent dev provisioning against the self-hosted Zitadel: ensures a project,
/// a PKCE SPA client with JWT access tokens (so the API validates via JWKS), and the two demo
/// reviewers. Authenticates with the machine-user PAT Zitadel writes on first-instance init.
/// Everything is find-or-create, so re-running against a persistent instance is safe.</summary>
internal static class ZitadelProvisioning
{
    private const string ProjectName = "ProductReviews";
    private const string ApplicationName = "productreviews-spa";

    public static readonly IReadOnlyList<(string UserName, string FirstName, string LastName)> DemoUsers =
    [
        ("demo@productreviews.local", "Demo", "Reviewer"),
        ("critic@productreviews.local", "Casey", "Critic"),
    ];

    public static async Task<string> ReadPatAsync(string patPath, string authority, CancellationToken cancellationToken)
    {
        // A cold Zitadel can take minutes before first-instance init writes the PAT — wait
        // patiently. But if it is already serving and still has no PAT after a short grace, the
        // instance predates the FIRSTINSTANCE PAT settings and never will write one — fail fast
        // with a reset hint instead of hanging.
        using var http = new HttpClient { BaseAddress = new Uri(authority) };
        var servingSeconds = 0;
        for (var attempt = 0; attempt < 300 && !cancellationToken.IsCancellationRequested; attempt++)
        {
            if (File.Exists(patPath))
            {
                var pat = (await File.ReadAllTextAsync(patPath, cancellationToken)).Trim();
                if (!string.IsNullOrEmpty(pat))
                {
                    return pat;
                }
            }

            servingSeconds = await IsServingAsync(http, cancellationToken) ? servingSeconds + 1 : 0;
            if (servingSeconds >= 15)
            {
                throw new InvalidOperationException(
                    $"Zitadel is serving but wrote no machine-user PAT at {patPath}. Reset it so init re-runs: " +
                    "remove the zitadel container and the Postgres data volume, then restart the AppHost.");
            }

            await Task.Delay(1000, cancellationToken);
        }
        throw new InvalidOperationException($"Zitadel PAT not found at {patPath} after 300s.");
    }

    public static async Task<string> EnsureSpaClientAsync(
        string authority, string pat, string frontendOrigin, Action<string> log, CancellationToken cancellationToken)
    {
        var redirectUris = new[] { $"{frontendOrigin}/auth/callback" };
        var postLogoutUris = new[] { frontendOrigin };

        using var http = CreateClient(authority, pat);

        await WaitForDiscoveryAsync(http, cancellationToken);
        await WaitForManagementReadyAsync(http, cancellationToken);

        var projectId = await FindProjectAsync(http, cancellationToken) ?? await CreateProjectAsync(http, cancellationToken);
        var (applicationId, clientId) = await FindApplicationAsync(http, projectId, cancellationToken);

        if (clientId is null)
        {
            clientId = await CreateApplicationAsync(http, projectId, redirectUris, postLogoutUris, cancellationToken);
            log($"Zitadel: created OIDC app '{ApplicationName}' (clientId {clientId})");
        }
        else
        {
            // Keep redirect URIs in step with the frontend origin across runs.
            await UpdateApplicationConfigAsync(http, projectId, applicationId!, redirectUris, postLogoutUris, cancellationToken);
            log($"Zitadel: reused OIDC app '{ApplicationName}' (clientId {clientId})");
        }

        return clientId;
    }

    public static async Task EnsureDemoUsersAsync(string authority, string pat, Action<string> log, CancellationToken cancellationToken)
    {
        using var http = CreateClient(authority, pat);

        foreach (var (userName, firstName, lastName) in DemoUsers)
        {
            if (await UserExistsAsync(http, userName, cancellationToken))
            {
                log($"Zitadel: demo user '{userName}' already exists");
                continue;
            }

            using var response = await http.PostAsJsonAsync("management/v1/users/human/_import", new
            {
                userName,
                profile = new { firstName, lastName, displayName = $"{firstName} {lastName}" },
                email = new { email = userName, isEmailVerified = true },
                password = ZitadelHosting.DemoUserPassword,
                passwordChangeRequired = false,
            }, cancellationToken);

            log(response.IsSuccessStatusCode
                ? $"Zitadel: created demo user '{userName}'"
                : $"Zitadel: demo user import failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}");
        }
    }

    private static HttpClient CreateClient(string authority, string pat)
    {
        var http = new HttpClient { BaseAddress = new Uri(authority) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        return http;
    }

    private static async Task<bool> UserExistsAsync(HttpClient http, string userName, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("management/v1/users/_search", new
        {
            queries = new object[] { new { userNameQuery = new { userName, method = "TEXT_QUERY_METHOD_EQUALS" } } },
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
        return document.RootElement.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.Array && result.GetArrayLength() > 0;
    }

    private static async Task WaitForDiscoveryAsync(HttpClient http, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 180 && !cancellationToken.IsCancellationRequested; attempt++)
        {
            if (await IsServingAsync(http, cancellationToken))
            {
                return;
            }
            await Task.Delay(1000, cancellationToken);
        }
        throw new InvalidOperationException("Zitadel OIDC discovery did not become reachable within 180s.");
    }

    private static async Task<bool> IsServingAsync(HttpClient http, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(".well-known/openid-configuration", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    // Discovery answers before the management backend is ready (first call would 503) — poll an
    // authenticated endpoint until it stops returning 5xx. Best-effort: on timeout the real calls
    // surface the error.
    private static async Task WaitForManagementReadyAsync(HttpClient http, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 120 && !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var response = await http.GetAsync("auth/v1/users/me", cancellationToken);
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            await Task.Delay(1000, cancellationToken);
        }
    }

    private static async Task<string?> FindProjectAsync(HttpClient http, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("management/v1/projects/_search", new
        {
            queries = new object[] { new { nameQuery = new { name = ProjectName, method = "TEXT_QUERY_METHOD_EQUALS" } } },
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
        if (document.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
        {
            foreach (var project in result.EnumerateArray())
            {
                if (project.TryGetProperty("id", out var id))
                {
                    return id.GetString();
                }
            }
        }
        return null;
    }

    private static async Task<string> CreateProjectAsync(HttpClient http, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("management/v1/projects", new { name = ProjectName }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
        return document.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task<(string? ApplicationId, string? ClientId)> FindApplicationAsync(
        HttpClient http, string projectId, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync($"management/v1/projects/{projectId}/apps/_search", new
        {
            queries = new object[] { new { nameQuery = new { name = ApplicationName, method = "TEXT_QUERY_METHOD_EQUALS" } } },
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
        if (document.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
        {
            foreach (var application in result.EnumerateArray())
            {
                var applicationId = application.TryGetProperty("id", out var id) ? id.GetString() : null;
                var clientId = application.TryGetProperty("oidcConfig", out var config) && config.TryGetProperty("clientId", out var cid)
                    ? cid.GetString()
                    : null;
                if (clientId is not null)
                {
                    return (applicationId, clientId);
                }
            }
        }
        return (null, null);
    }

    private static async Task<string> CreateApplicationAsync(
        HttpClient http, string projectId, string[] redirectUris, string[] postLogoutUris, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(
            $"management/v1/projects/{projectId}/apps/oidc",
            OidcConfigBody(redirectUris, postLogoutUris, withName: true),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
        return document.RootElement.GetProperty("clientId").GetString()!;
    }

    private static async Task UpdateApplicationConfigAsync(
        HttpClient http, string projectId, string applicationId, string[] redirectUris, string[] postLogoutUris,
        CancellationToken cancellationToken)
    {
        using var response = await http.PutAsJsonAsync(
            $"management/v1/projects/{projectId}/apps/{applicationId}/oidc_config",
            OidcConfigBody(redirectUris, postLogoutUris, withName: false),
            cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Zitadel rejects a no-op update with 400 "No changes" — which is success here.
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest && body.Contains("No changes", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        throw new HttpRequestException($"Updating OIDC app config failed ({(int)response.StatusCode}): {body}");
    }

    // A public SPA client: authorization code + PKCE, no secret. USER_AGENT (not WEB) so Zitadel
    // serves CORS on the token endpoint for the registered redirect origins — the browser itself
    // exchanges the code. JWT access tokens so the API validates via JWKS. devMode relaxes the
    // https requirement for localhost redirect URIs.
    private static object OidcConfigBody(string[] redirectUris, string[] postLogoutUris, bool withName)
    {
        var config = new Dictionary<string, object?>
        {
            ["redirectUris"] = redirectUris,
            ["responseTypes"] = new[] { "OIDC_RESPONSE_TYPE_CODE" },
            ["grantTypes"] = new[] { "OIDC_GRANT_TYPE_AUTHORIZATION_CODE", "OIDC_GRANT_TYPE_REFRESH_TOKEN" },
            ["appType"] = "OIDC_APP_TYPE_USER_AGENT",
            ["authMethodType"] = "OIDC_AUTH_METHOD_TYPE_NONE",
            ["postLogoutRedirectUris"] = postLogoutUris,
            ["devMode"] = true,
            ["accessTokenType"] = "OIDC_TOKEN_TYPE_JWT",
            ["accessTokenRoleAssertion"] = true,
        };
        if (withName)
        {
            config["name"] = ApplicationName;
        }
        return config;
    }
}
