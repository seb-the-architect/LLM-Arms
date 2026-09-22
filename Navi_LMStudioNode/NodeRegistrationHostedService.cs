using System.Net.Http.Json;
using System.Text.Json;
using Navi_Protocol;

namespace Navi_LMStudioNode;

public sealed class NodeRegistrationHostedService : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<NodeRegistrationHostedService> _logger;
    private readonly LmStudioNode _node;

    public NodeRegistrationHostedService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IHostApplicationLifetime applicationLifetime,
        ILogger<NodeRegistrationHostedService> logger,
        LmStudioNode node)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _node = node;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForApplicationStartedAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (await TryRegisterAsync(stoppingToken))
            {
                return;
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }
    }

    private async Task<bool> TryRegisterAsync(CancellationToken cancellationToken)
    {
        string? hubBaseUrl = _configuration["Hub:BaseUrl"];
        string? nodeAddressValue = _configuration["Node:Address"];

        if (!Uri.TryCreate(hubBaseUrl, UriKind.Absolute, out Uri? hubUri) ||
            !Uri.TryCreate(nodeAddressValue, UriKind.Absolute, out Uri? nodeUri))
        {
            _logger.LogError(
                "Node registration requires valid absolute Hub:BaseUrl and Node:Address values.");
            return false;
        }

        var registrationRequest = new NodeRegistrationRequest
        {
            NodeDescription = _node.NodeDescription,
            NodeAddress = nodeUri
        };

        try
        {
            using HttpClient client = _httpClientFactory.CreateClient();
            client.BaseAddress = hubUri;
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/nodes/register",
                registrationRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Node registration failed with HTTP status {StatusCode}.",
                    (int)response.StatusCode);
                return false;
            }

            NodeRegistrationResponse? registrationResponse =
                await response.Content.ReadFromJsonAsync<NodeRegistrationResponse>(
                    cancellationToken: cancellationToken);

            if (registrationResponse?.RegistrationSuccess != NodeRegistrationResponseStatus.Success)
            {
                _logger.LogWarning("The Hub returned an invalid or unsuccessful registration response.");
                return false;
            }

            if (TryGetNodeGuid(response.Headers.Location, out Guid nodeGuid))
            {
                _logger.LogInformation("Node registration succeeded with Guid {NodeGuid}.", nodeGuid);
            }
            else
            {
                _logger.LogInformation("Node registration succeeded, but the response did not include a node Guid.");
            }

            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("The node registration request timed out.");
            return false;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                "Unable to contact the Hub for node registration: {ErrorMessage}",
                exception.Message);
            return false;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The Hub returned an invalid node registration response body.");
            return false;
        }
        catch (NotSupportedException exception)
        {
            _logger.LogWarning(exception, "The Hub returned an unsupported node registration response body.");
            return false;
        }
    }

    private Task WaitForApplicationStartedAsync(CancellationToken cancellationToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _applicationLifetime.ApplicationStarted.Register(() => started.TrySetResult());
        return started.Task.WaitAsync(cancellationToken);
    }

    private static bool TryGetNodeGuid(Uri? location, out Guid nodeGuid)
    {
        string? finalSegment = location?.OriginalString
            .TrimEnd('/')
            .Split('/')
            .LastOrDefault();
        return Guid.TryParse(finalSegment, out nodeGuid);
    }
}
