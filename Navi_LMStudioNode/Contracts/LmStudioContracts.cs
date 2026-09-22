using System.Text.Json.Serialization;

namespace Navi_LMStudioNode.Contracts;

internal sealed record ModelsResponse
{
    [JsonPropertyName("models")]
    public required IReadOnlyList<ModelInfo> Models { get; init; }
}

internal sealed record ModelInfo
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("loaded_instances")]
    public required IReadOnlyList<LoadedModelInstance> LoadedInstances { get; init; }
}

internal sealed record LoadedModelInstance
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

internal sealed record LoadModelRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("context_length")]
    public required int ContextLength { get; init; }
}

internal sealed record LoadModelResponse
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("instance_id")]
    public required string InstanceId { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}

internal sealed record ChatRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("input")]
    public required string Input { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    [JsonPropertyName("store")]
    public bool Store { get; init; }

    [JsonPropertyName("max_output_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxOutputTokens { get; init; }
}

internal sealed record ChatResponse
{
    [JsonPropertyName("model_instance_id")]
    public required string ModelInstanceId { get; init; }

    [JsonPropertyName("output")]
    public required IReadOnlyList<ChatOutput> Output { get; init; }
}

internal sealed record ChatOutput
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }
}
