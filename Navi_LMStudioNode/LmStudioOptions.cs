namespace Navi_LMStudioNode;

public sealed record LmStudioOptions
{
    public required Uri BaseUrl { get; init; }
    public required string Model { get; init; }
    public required int ContextLength { get; init; }
    public string? ApiToken { get; init; }

    public static LmStudioOptions FromConfiguration(IConfiguration configuration)
    {
        string? configuredBaseUrl = configuration["LmStudio:BaseUrl"];
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out Uri? baseUrl) ||
            (baseUrl.Scheme != Uri.UriSchemeHttp &&
             baseUrl.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(baseUrl.Query) ||
            !string.IsNullOrEmpty(baseUrl.Fragment))
        {
            throw new InvalidOperationException(
                "LM Studio configuration is invalid: LmStudio:BaseUrl must be an absolute HTTP or HTTPS URI without a query or fragment.");
        }

        string? model = configuration["LmStudio:Model"];
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                "LM Studio configuration is invalid: LmStudio:Model is required.");
        }

        int? contextLength = configuration.GetValue<int?>("LmStudio:ContextLength");
        if (contextLength is null or <= 0)
        {
            throw new InvalidOperationException(
                "LM Studio configuration is invalid: LmStudio:ContextLength must be a positive integer.");
        }

        string? apiToken = configuration["LmStudio:ApiToken"];

        return new LmStudioOptions
        {
            BaseUrl = new Uri(baseUrl.AbsoluteUri.TrimEnd('/') + "/"),
            Model = model,
            ContextLength = contextLength.Value,
            ApiToken = string.IsNullOrWhiteSpace(apiToken) ? null : apiToken
        };
    }
}
