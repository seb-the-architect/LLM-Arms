using Navi_Protocol;

namespace Navi_Hub.Models;

public class RegisteredService
{
    // A unique identifier for this service
    public required Guid ServiceGuid { get; init; }
    
    // The description of the service
    public required ServiceDescription ServiceDescription { get; init; }
}