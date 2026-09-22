using System.Text.Json.Serialization;

namespace Navi_Protocol;

public sealed record ServiceChainRequest
{
    // The list of service steps in sequential order
    [JsonPropertyName("steps")]
    public required List<ServiceChainStep> Steps { get; init; }
}
