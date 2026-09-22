using System.Text.Json.Serialization;

namespace Navi_Protocol;

public sealed record ServiceChainStep
{
    // The requested service
    [JsonPropertyName("service_request")]
    public required ServiceRequest ServiceRequest { get; init; }
    
    // A chain-local, human-friendly identifier for this service call.
    // This is used for referencing return values to pass to other requests in the chain
    [JsonPropertyName("call_key")]
    public string? CallKey { get; init; }
}
