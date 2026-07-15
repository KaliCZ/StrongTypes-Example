using ProductReviews.AppHost.Zitadel;

var builder = DistributedApplication.CreateBuilder(args);

// E2E mode (set by the Playwright harness): throwaway containers so every run starts clean,
// and the frontend serves a production build on a pinned port the tests know up front.
var e2e = string.Equals(builder.Configuration["ProductReviews:E2E"], "true", StringComparison.OrdinalIgnoreCase);

var postgresUsername = builder.AddParameter("postgres-username", "postgres");
var postgresPassword = builder.AddParameter("postgres-password", "postgres-dev-password", secret: true);

var postgres = e2e
    ? builder.AddPostgres("postgres", postgresUsername, postgresPassword)
    : builder.AddPostgres("postgres", postgresUsername, postgresPassword)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("productreviews-postgres")
        .WithDataVolume("productreviews-postgres-data");

var productReviewsDatabase = postgres.AddDatabase("productreviews");
var zitadelDatabase = postgres.AddDatabase("zitadel");

// --- Identity provider: self-hosted Zitadel (ADR-0005) -----------------------
// Zitadel writes its first-instance machine-user PATs into this bind-mounted dir on first
// init; the AppHost reads them to drive provisioning. Gitignored.
var repositoryRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var zitadelKeyDirectory = Path.Combine(repositoryRoot, ".zitadel");
Directory.CreateDirectory(zitadelKeyDirectory);
// Zitadel runs as a non-root uid and must be able to create files in the bind mount on Linux;
// Docker Desktop (Windows/macOS) ignores bind-mount permissions, so this is a no-op there.
if (!OperatingSystem.IsWindows())
{
    File.SetUnixFileMode(zitadelKeyDirectory,
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
        UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
}
var patPath = Path.Combine(zitadelKeyDirectory, "pat.txt");

var zitadel = builder.AddZitadel(postgresUsername, postgresPassword, postgres.GetEndpoint("tcp"), zitadelDatabase, zitadelKeyDirectory);
if (!e2e)
{
    zitadel.WithContainerName("productreviews-zitadel").WithLifetime(ContainerLifetime.Persistent);
}

var zitadelHttp = zitadel.GetEndpoint("http");
var zitadelAuthority = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(zitadel.Resource, (_, _) =>
{
    zitadelAuthority.TrySetResult(zitadelHttp.Url);
    return Task.CompletedTask;
});

var zitadelLogin = builder.AddZitadelLogin(zitadel, zitadelKeyDirectory);
if (!e2e)
{
    zitadelLogin.WithContainerName("productreviews-zitadel-login").WithLifetime(ContainerLifetime.Persistent);
}

// The SPA client id produced by provisioning; the api (token audience) and the frontend
// (login client id) both await it.
var oidc = new ZitadelOidc();

// --- API ----------------------------------------------------------------------
// launchProfileName: null bypasses launchSettings (dynamic port, so the frontend can reference
// it) — which also drops ASPNETCORE_ENVIRONMENT, so set it explicitly.
var api = builder.AddProject<Projects.ProductReviews_Api>("api", launchProfileName: null)
    .WithHttpEndpoint()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(productReviewsDatabase).WaitFor(productReviewsDatabase)
    .WaitFor(zitadel)
    .WithEnvironment(async context =>
    {
        context.EnvironmentVariables["Oidc__Authority"] = await zitadelAuthority.Task;

        // Audience = the provisioned SPA client id. Falls back to issuer-only validation if
        // provisioning hasn't produced one (sign-in would be broken anyway; the API still serves reads).
        var done = await Task.WhenAny(oidc.ClientId.Task, Task.Delay(TimeSpan.FromSeconds(120), context.CancellationToken));
        if (done == oidc.ClientId.Task && !string.IsNullOrEmpty(oidc.ClientId.Task.Result))
        {
            context.EnvironmentVariables["Oidc__Audience"] = oidc.ClientId.Task.Result;
        }
    });

// --- Frontend: Vue 3 + Vite SPA (ADR-0008) -------------------------------------
// AddNpmApp only runs the script — install once when node_modules is absent and gate on it.
var frontendDirectory = Path.Combine(repositoryRoot, "frontend");
IResourceBuilder<ExecutableResource>? frontendInstall = null;
if (!Directory.Exists(Path.Combine(frontendDirectory, "node_modules")))
{
    var npm = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
    frontendInstall = builder.AddExecutable("frontend-install", npm, frontendDirectory, "ci");
}

var frontend = builder.AddNpmApp("frontend", "../../frontend", e2e ? "preview" : "dev")
    .WithEnvironment("API_PROXY_TARGET", api.GetEndpoint("http"))
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: e2e ? 4173 : 5173, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment(async context =>
    {
        // Browser-facing OIDC config, baked into the Vite dev/preview server's env before start.
        context.EnvironmentVariables["VITE_OIDC_AUTHORITY"] = await zitadelAuthority.Task;
        context.EnvironmentVariables["VITE_OIDC_CLIENT_ID"] = await oidc.ClientId.Task;
    });

if (frontendInstall is not null)
{
    frontend.WaitForCompletion(frontendInstall);
}

// Provision once the frontend's endpoint is allocated (allocation happens before start and is
// not gated by WaitFor — provisioning on frontend START would deadlock, because the frontend
// waits for the api, and the api's env awaits the client id this produces). Fire-and-forget:
// provisioning itself waits for Zitadel to come up.
var frontendHttp = frontend.GetEndpoint("http");
builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(frontend.Resource, (_, _) =>
{
    var frontendOrigin = frontendHttp.Url;
    _ = Task.Run(
        async () =>
    {
        try
        {
            var authority = await zitadelAuthority.Task;
            var pat = await ZitadelProvisioning.ReadPatAsync(patPath, authority, CancellationToken.None);
            var clientId = await ZitadelProvisioning.EnsureSpaClientAsync(
                authority, pat, frontendOrigin, Console.WriteLine, CancellationToken.None);
            oidc.ClientId.TrySetResult(clientId);

            // Isolated so a seeding hiccup never blocks sign-in for existing users.
            try
            {
                await ZitadelProvisioning.EnsureDemoUsersAsync(authority, pat, Console.WriteLine, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Zitadel demo-user seeding failed (existing users can still sign in): {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Zitadel provisioning failed; sign-in will be unavailable: {exception.Message}");
            oidc.ClientId.TrySetResult("");
        }
    },
        CancellationToken.None);
    return Task.CompletedTask;
});

builder.Build().Run();
