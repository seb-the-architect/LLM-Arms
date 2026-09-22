using Navi_Protocol;

namespace Navi_Hub.Models;

public class RegisteredNode
{
    // A unique identifier for this node
    public required Guid NodeGuid { get; init; }
       
    // The description of the node
    public required NodeDescription NodeDescription { get; init; }

    // The URI and port on which the node listens for service requests
    public required Uri NodeAddress { get; init; }
}
