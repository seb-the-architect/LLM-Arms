namespace Navi_Protocol;

public sealed record NodeDescription
{
    // The unique name of the node
    public required string Name { get; init; }
    
    // A summary of what kind of services the node offers
    public required string Summary { get; init; }
    
    // Descriptions of what services the node offers
    public required IReadOnlyList<ServiceDescription> ServiceDescriptions { get; init; }
}