using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Navi_Protocol;

public sealed record ServiceRequest
{
    // The name of the node that owns the service
    [JsonPropertyName("target_node_name")]
    public required string TargetNodeName { get; init; }

    // The name of the service to request
    [JsonPropertyName("target_service_name")]
    public required string TargetServiceName { get; init; }
    
    // The parameters passed for this service
    [JsonPropertyName("parameters")]
    public required JsonObject Parameters { get; init; }
}
