namespace Navi_Protocol;

public sealed record ServiceDescription
{
    // The unique name of the service
    public required string ServiceName { get; init; }
    
    // A summary of what the service does
    public required string Summary { get; init; }
    
    // What parameters are needed and a summary of each
    public required IReadOnlyList<ServiceParameterDescription> Parameters { get; init; }
    
    // What values this service returns
    public required IReadOnlyList<ServiceReturnValueDescription> ReturnValues { get; init; }
}

public sealed record ServiceParameterDescription
{
    public required string Name { get; init; }

    public required string Summary { get; init; }

    public bool Required { get; init; } = true;
    
    public required ServiceValueType ParameterType { get; init; }
}

public sealed record ServiceReturnValueDescription
{
    public required string Name { get; init; }

    public required string Summary { get; init; }

    public required ServiceValueType ReturnType { get; init; }
}
