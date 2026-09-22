using Navi_LMStudioNode.Contracts;
using Navi_LMStudioNode.Services;

namespace Navi_LMStudioNode;

internal sealed class LmStudioStartupHostedService : IHostedService
{
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);

    private readonly LmStudioClient _client;
    private readonly ILogger<LmStudioStartupHostedService> _logger;
    private readonly LmStudioNode _node;
    private readonly LmStudioOptions _options;
    private readonly TimeSpan _retryDelay;

    public LmStudioStartupHostedService(
        LmStudioClient client,
        ILogger<LmStudioStartupHostedService> logger,
        LmStudioNode node,
        LmStudioOptions options,
        TimeSpan? retryDelay = null)
    {
        _client = client;
        _logger = logger;
        _node = node;
        _options = options;
        _retryDelay = retryDelay ?? DefaultRetryDelay;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Checking LM Studio at {BaseUrl} for model {Model}.",
                _options.BaseUrl,
                _options.Model);

            IReadOnlyList<ModelInfo> models =
                await ConnectAsync(cancellationToken);
            ModelInfo model = models.SingleOrDefault(candidate =>
                                  string.Equals(
                                      candidate.Key,
                                      _options.Model,
                                      StringComparison.Ordinal))
                              ?? throw new InvalidOperationException(
                                  $"The configured LM Studio model '{_options.Model}' is not available.");

            if (!string.Equals(model.Type, "llm", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The configured LM Studio model '{_options.Model}' is not an LLM.");
            }

            string modelInstanceId;
            if (model.LoadedInstances.Count > 0)
            {
                modelInstanceId = model.LoadedInstances[0].Id;
                _logger.LogInformation(
                    "LM Studio model {Model} is already loaded as {ModelInstanceId}.",
                    _options.Model,
                    modelInstanceId);
            }
            else
            {
                _logger.LogInformation(
                    "Loading LM Studio model {Model} with context length {ContextLength}.",
                    _options.Model,
                    _options.ContextLength);

                LoadModelResponse loadResponse = await _client.LoadModelAsync(
                    _options.Model,
                    _options.ContextLength,
                    cancellationToken);

                if (!string.Equals(loadResponse.Status, "loaded", StringComparison.Ordinal) ||
                    !string.Equals(loadResponse.Type, "llm", StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(loadResponse.InstanceId))
                {
                    throw new InvalidOperationException(
                        $"LM Studio did not report model '{_options.Model}' as loaded.");
                }

                modelInstanceId = loadResponse.InstanceId;
            }

            models = await _client.GetModelsAsync(cancellationToken);
            bool isReady = models.Any(candidate =>
                string.Equals(candidate.Key, _options.Model, StringComparison.Ordinal) &&
                candidate.LoadedInstances.Any(instance =>
                    string.Equals(instance.Id, modelInstanceId, StringComparison.Ordinal)));
            if (!isReady)
            {
                throw new InvalidOperationException(
                    $"LM Studio model '{_options.Model}' is not ready for inference.");
            }

            ChatResponse probe = await _client.ChatAsync(
                modelInstanceId,
                "Reply with OK.",
                maxOutputTokens: null,
                cancellationToken);
            if (!string.Equals(
                    probe.ModelInstanceId,
                    modelInstanceId,
                    StringComparison.Ordinal) ||
                !probe.Output.Any(item =>
                    item.Type == "message" &&
                    !string.IsNullOrWhiteSpace(item.Content)))
            {
                throw new InvalidOperationException(
                    $"LM Studio model '{_options.Model}' failed the inference readiness check.");
            }

            _node.ModelInstanceId = modelInstanceId;
            _logger.LogInformation(
                "LM Studio model {Model} is ready for inference as {ModelInstanceId}.",
                _options.Model,
                modelInstanceId);
        }
        catch (Exception exception) when (
            exception is HttpRequestException
                or System.Text.Json.JsonException
                or InvalidOperationException)
        {
            _logger.LogCritical(
                exception,
                "LM Studio node startup failed; the node will not register with the Hub.");
            throw;
        }
    }

    private async Task<IReadOnlyList<ModelInfo>> ConnectAsync(
        CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                return await _client.GetModelsAsync(cancellationToken);
            }
            catch (HttpRequestException exception) when (
                exception.StatusCode is null)
            {
                _logger.LogWarning(
                    "Unable to connect to LM Studio at {BaseUrl}: {ErrorMessage}. Retrying in {RetryDelaySeconds} seconds.",
                    _options.BaseUrl,
                    exception.Message,
                    _retryDelay.TotalSeconds);
            }
            catch (TaskCanceledException exception) when (
                !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "The connection attempt to LM Studio at {BaseUrl} timed out: {ErrorMessage}. Retrying in {RetryDelaySeconds} seconds.",
                    _options.BaseUrl,
                    exception.Message,
                    _retryDelay.TotalSeconds);
            }

            await Task.Delay(_retryDelay, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
