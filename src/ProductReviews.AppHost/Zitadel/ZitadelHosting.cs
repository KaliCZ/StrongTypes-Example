namespace ProductReviews.AppHost.Zitadel;

/// <summary>The self-hosted Zitadel containers (ADR-0005): the core (API + Console) and the
/// separate Login V2 app the core redirects browsers to for the hosted sign-in page.</summary>
internal static class ZitadelHosting
{
    public const string Image = "ghcr.io/zitadel/zitadel";
    public const string Tag = "v4.15.1";
    public const string LoginImage = "ghcr.io/zitadel/zitadel-login";

    // Dev-only masterkey (encrypts secrets at rest) — a real deployment supplies its own.
    public const string DevMasterkey = "MasterkeyNeedsToHave32Characters";

    // Fixed host port so the OIDC issuer is the stable http://localhost:8090 the browser,
    // the SPA, and the API all agree on.
    public const int CorePort = 8090;

    public const int LoginPort = 3001;
    public const string LoginBaseUri = "http://localhost:3001/ui/v2/login/";

    // The core writes the IAM_LOGIN_CLIENT machine user's PAT here on first init
    // (bind-mounted dir shared with the login container).
    public const string LoginClientPatContainerPath = "/machinekey/login-client.pat";

    /// <summary>Password for the two seeded demo reviewers; meets Zitadel's default complexity policy.
    /// Documented in the README — local demo only.</summary>
    public const string DemoUserPassword = "ProductReviews123!";

    public static IResourceBuilder<ContainerResource> AddZitadel(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ParameterResource> postgresUsername,
        IResourceBuilder<ParameterResource> postgresPassword,
        EndpointReference postgresTcp,
        IResourceBuilder<PostgresDatabaseResource> zitadelDatabase,
        string machineKeyDirectory) =>
        builder.AddContainer("zitadel", Image, Tag)
            .WithArgs("start-from-init", "--masterkeyFromEnv", "--tlsMode", "disabled")
            .WithHttpEndpoint(port: CorePort, targetPort: 8080, name: "http")
            .WithBindMount(machineKeyDirectory, "/machinekey")
            .WithEnvironment("ZITADEL_MASTERKEY", DevMasterkey)
            .WithEnvironment("ZITADEL_EXTERNALSECURE", "false")
            .WithEnvironment("ZITADEL_EXTERNALDOMAIN", "localhost")
            .WithEnvironment("ZITADEL_EXTERNALPORT", CorePort.ToString())
            .WithEnvironment("ZITADEL_TLS_ENABLED", "false")
            // First-instance machine user + long-lived PAT, written to the bind-mounted
            // /machinekey/pat.txt. Applied only on the very first init of a fresh instance.
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_PATPATH", "/machinekey/pat.txt")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_MACHINE_MACHINE_USERNAME", "productreviews-admin-sa")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_MACHINE_MACHINE_NAME", "ProductReviews Admin SA")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_MACHINE_PAT_EXPIRATIONDATE", "2100-01-01T00:00:00Z")
            // Login V2: a second machine user (IAM_LOGIN_CLIENT) whose PAT the login container
            // reads; the feature is switched on at init with the login app's URLs baked in.
            // These settings only take effect on a FRESH init — changing them requires resetting
            // the zitadel container and its database.
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_LOGINCLIENTPATPATH", LoginClientPatContainerPath)
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_MACHINE_USERNAME", "login-client")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_MACHINE_NAME", "Login Client")
            .WithEnvironment("ZITADEL_FIRSTINSTANCE_ORG_LOGINCLIENT_PAT_EXPIRATIONDATE", "2100-01-01T00:00:00Z")
            .WithEnvironment("ZITADEL_DEFAULTINSTANCE_FEATURES_LOGINV2_REQUIRED", "true")
            .WithEnvironment("ZITADEL_DEFAULTINSTANCE_FEATURES_LOGINV2_BASEURI", LoginBaseUri)
            .WithEnvironment("ZITADEL_OIDC_DEFAULTLOGINURLV2", $"{LoginBaseUri}login?authRequest=")
            .WithEnvironment("ZITADEL_OIDC_DEFAULTLOGOUTURLV2", $"{LoginBaseUri}logout?post_logout_redirect=")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_DATABASE", "zitadeldb")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_USERNAME", postgresUsername)
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_PASSWORD", postgresPassword)
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_USER_SSL_MODE", "disable")
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_USERNAME", postgresUsername)
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_PASSWORD", postgresPassword)
            .WithEnvironment("ZITADEL_DATABASE_POSTGRES_ADMIN_SSL_MODE", "disable")
            .WithEnvironment(context =>
            {
                // Host/port as seen from inside the Aspire container network.
                context.EnvironmentVariables["ZITADEL_DATABASE_POSTGRES_HOST"] = postgresTcp.Property(EndpointProperty.Host);
                context.EnvironmentVariables["ZITADEL_DATABASE_POSTGRES_PORT"] = postgresTcp.Property(EndpointProperty.Port);
            })
            .WaitFor(zitadelDatabase);

    public static IResourceBuilder<ContainerResource> AddZitadelLogin(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ContainerResource> zitadel,
        string machineKeyDirectory)
    {
        var coreHttp = zitadel.GetEndpoint("http");
        var loginClientPatPath = Path.Combine(machineKeyDirectory, "login-client.pat");
        return builder.AddContainer("zitadel-login", LoginImage, Tag)
            .WithHttpEndpoint(port: LoginPort, targetPort: 3000, name: "http")
            .WithBindMount(machineKeyDirectory, "/machinekey")
            .WithEnvironment("NEXT_PUBLIC_BASE_PATH", "/ui/v2/login")
            .WithEnvironment("ZITADEL_SERVICE_USER_TOKEN_FILE", LoginClientPatContainerPath)
            // Zitadel resolves the instance from the host header, so back-channel calls over the
            // container network must present the public host.
            .WithEnvironment("CUSTOM_REQUEST_HEADERS", (string)$"Host:localhost:{CorePort},X-Forwarded-Proto:http")
            .WithEnvironment(async context =>
            {
                context.EnvironmentVariables["ZITADEL_API_URL"] =
                    ReferenceExpression.Create($"http://{coreHttp.Property(EndpointProperty.Host)}:{coreHttp.Property(EndpointProperty.Port)}");

                // On a cold init the core writes the PAT only after first-instance setup finishes,
                // and the login app exits immediately when the file is missing — so block start
                // until it lands (WaitFor only gates on the container running, not on init done).
                for (var attempt = 0; attempt < 300 && !context.CancellationToken.IsCancellationRequested; attempt++)
                {
                    if (File.Exists(loginClientPatPath))
                    {
                        return;
                    }
                    await Task.Delay(1000, context.CancellationToken);
                }
            })
            .WaitFor(zitadel);
    }
}
