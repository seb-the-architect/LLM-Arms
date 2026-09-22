using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Navi_Protocol;

public sealed record ServiceResponse
{
    // The status that the task resolved to.
    [JsonPropertyName("status")]
    public required TaskStatus Status { get; init; }

    // The values returned by the service execution.
    // An empty block if the service failed or returned no useful data.
    [JsonPropertyName("return_values")]
    public JsonObject ReturnValues { get; init; } = new();

    // Errors returned by the service execution.
    [JsonPropertyName("errors")]
    public List<ServiceError> Errors { get; init; } = [];
}

public enum TaskStatus
{
    Success,
    Failed,
    Denied,
    TimedOut
}

public sealed record ServiceError
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("recoverable")]
    public bool Recoverable { get; init; }
}
