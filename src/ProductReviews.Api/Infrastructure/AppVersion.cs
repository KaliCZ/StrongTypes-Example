using System.Reflection;

namespace ProductReviews.Api.Infrastructure;

public static class AppVersion
{
    // The SDK embeds the git commit as the SourceRevisionId ("<version>+<sha>") when
    // building from a checkout; a deploy gate reads it back out of /health.
    public static readonly string InformationalVersion =
        Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

    public static readonly string CommitHash =
        InformationalVersion.Contains('+')
            ? InformationalVersion[(InformationalVersion.IndexOf('+') + 1)..]
            : InformationalVersion;
}
