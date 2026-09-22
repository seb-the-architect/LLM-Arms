namespace Navi_Protocol;

public sealed record NodeRegistrationRequest
{
    // A description of the node
    public required NodeDescription NodeDescription { get; init; }
    
    // The URI of the Node on the Navi Network and the port on which it listens for service requests
    public required Uri NodeAddress { get; init; }
}
