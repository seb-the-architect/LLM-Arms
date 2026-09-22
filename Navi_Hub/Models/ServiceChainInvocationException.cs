namespace Navi_Hub.Models;

public sealed class ServiceChainInvocationException : Exception
{
    public ServiceChainInvocationException(
        int statusCode,
        string code,
        string message,
        string? callKey = null,
        string? targetNodeName = null,
        string? targetServiceName = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Code = code;
        CallKey = callKey;
        TargetNodeName = targetNodeName;
        TargetServiceName = targetServiceName;
    }

    public int StatusCode { get; }

    public string Code { get; }

    public string? CallKey { get; }

    public string? TargetNodeName { get; }

    public string? TargetServiceName { get; }
}
