using Tomlyn.Extensions.Configuration;

namespace Navi_LMStudioNode;

public static class LmStudioNodeConfiguration
{
    public static void Configure(
        ConfigurationManager configuration,
        string contentRootPath,
        string environmentName,
        string[] args)
    {
        configuration.Sources.Clear();

        configuration
            .SetBasePath(contentRootPath)
            .AddTomlFile("appsettings.toml", optional: false, reloadOnChange: false)
            .AddTomlFile(
                $"appsettings.{environmentName}.toml",
                optional: true,
                reloadOnChange: false)
            .AddTomlFile(
                "appsettings.local.toml",
                optional: true,
                reloadOnChange: false)
            .AddEnvironmentVariables();

        if (args.Length > 0)
        {
            configuration.AddCommandLine(args);
        }
    }

    public static Uri GetNodeAddress(IConfiguration configuration)
    {
        string? configuredAddress = configuration["Node:Address"];
        if (!Uri.TryCreate(configuredAddress, UriKind.Absolute, out Uri? nodeAddress) ||
            (nodeAddress.Scheme != Uri.UriSchemeHttp &&
             nodeAddress.Scheme != Uri.UriSchemeHttps) ||
            nodeAddress.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(nodeAddress.Query) ||
            !string.IsNullOrEmpty(nodeAddress.Fragment))
        {
            throw new InvalidOperationException(
                "Node configuration is invalid: Node:Address must be an absolute HTTP or HTTPS URI without a path, query, or fragment.");
        }

        return nodeAddress;
    }
}
