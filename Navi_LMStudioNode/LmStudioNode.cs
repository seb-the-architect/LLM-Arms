using Navi_LMStudioNode.Services;
using Navi_Protocol;
using Navi_ServiceSdk;

namespace Navi_LMStudioNode;

public sealed class LmStudioNode : Node
{
    public LmStudioNode(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        LmStudioOptions options,
        ILogger<Respond> respondLogger)
    {
        string? nodeName = configuration["Node:Name"];
        if (string.IsNullOrWhiteSpace(nodeName))
        {
            throw new InvalidOperationException(
                "Node configuration is invalid: Node:Name is required.");
        }

        var client = new LmStudioClient(
            httpClientFactory.CreateClient(nameof(LmStudioNode)),
            options);
        Services = [new Respond(this, client, respondLogger)];

        NodeDescription = new NodeDescription
        {
            Name = nodeName,
            Summary = "Generates responses using LM Studio.",
            ServiceDescriptions = Services
                .Select(service => service.ServiceDescription)
                .ToArray()
        };
    }

    public override NodeDescription NodeDescription { get; }

    public override IReadOnlyList<Service> Services { get; }

    internal string? ModelInstanceId { get; set; }
}
