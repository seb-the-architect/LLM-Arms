namespace Navi_Protocol;

public sealed record NodeRegistrationResponse
{
    // Whether the registration was successful
    public required NodeRegistrationResponseStatus RegistrationSuccess { get; init; }
}

public enum NodeRegistrationResponseStatus
{
    Success,
    Failure
}