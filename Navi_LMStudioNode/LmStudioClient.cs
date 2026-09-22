using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Navi_LMStudioNode.Contracts;

namespace Navi_LMStudioNode;

internal sealed class LmStudioClient
{
    private readonly HttpClient _httpClient;

    public LmStudioClient(HttpClient httpClient, LmStudioOptions options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = options.BaseUrl;

        if (options.ApiToken is not null)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiToken);
        }
    }

    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response =
            await _httpClient.GetAsync("api/v1/models", cancellationToken);
        ModelsResponse body = await ReadSuccessAsync<ModelsResponse>(
            response,
            cancellationToken);
        return body.Models;
    }

    public async Task<LoadModelResponse> LoadModelAsync(
        string model,
        int contextLength,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "api/v1/models/load",
            new LoadModelRequest
            {
                Model = model,
                ContextLength = contextLength
            },
            cancellationToken);

        return await ReadSuccessAsync<LoadModelResponse>(response, cancellationToken);
    }

    public async Task<ChatResponse> ChatAsync(
        string modelInstanceId,
        string prompt,
        int? maxOutputTokens,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "api/v1/chat",
            new ChatRequest
            {
                Model = modelInstanceId,
                Input = prompt,
                Stream = false,
                Store = false,
                MaxOutputTokens = maxOutputTokens
            },
            cancellationToken);

        return await ReadSuccessAsync<ChatResponse>(response, cancellationToken);
    }

    private static async Task<T> ReadSuccessAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"LM Studio returned HTTP {(int)response.StatusCode}: {errorBody}",
                inner: null,
                response.StatusCode);
        }

        try
        {
            T? body = await response.Content.ReadFromJsonAsync<T>(
                cancellationToken: cancellationToken);
            return body ?? throw new JsonException("LM Studio returned an empty response body.");
        }
        catch (NotSupportedException exception)
        {
            throw new JsonException("LM Studio returned an unsupported response body.", exception);
        }
    }
}
