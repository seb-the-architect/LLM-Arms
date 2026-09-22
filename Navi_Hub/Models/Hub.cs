using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Navi_Protocol;

namespace Navi_Hub.Models;

public sealed class Hub : IDisposable
{
    private const string ResultReferencePropertyName = "$result";
    private const string CallKeyPropertyName = "call_key";
    private const string ReturnValuePropertyName = "return_value";

    private readonly HttpServiceRequestForwarder _serviceRequestForwarder;
    private readonly ConcurrentDictionary<string, RegisteredNode> _registeredNodes =
        new(StringComparer.OrdinalIgnoreCase);

    public Hub()
        : this(new HttpServiceRequestForwarder())
    {
    }

    internal Hub(HttpClient httpClient)
        : this(new HttpServiceRequestForwarder(httpClient))
    {
    }

    private Hub(HttpServiceRequestForwarder serviceRequestForwarder)
    {
        _serviceRequestForwarder = serviceRequestForwarder;
    }

    // A snapshot of the nodes that are currently registered with the Hub
    public IReadOnlyCollection<RegisteredNode> RegisteredNodes => _registeredNodes.Values.ToArray();
    
    // Register a Node
    public (NodeRegistrationResponse Response, RegisteredNode? RegisteredNode) RegisterNode(
        NodeRegistrationRequest registrationRequest)
    {
        // Create the new registered node instance
        RegisteredNode registeredNode = new RegisteredNode
        {
            NodeGuid = Guid.NewGuid(),
            NodeDescription = registrationRequest.NodeDescription,
            NodeAddress = registrationRequest.NodeAddress
        };

        // Add the newly registered node unless its name is already registered
        if (!_registeredNodes.TryAdd(registeredNode.NodeDescription.Name, registeredNode))
        {
            return (
                new NodeRegistrationResponse
                {
                    RegistrationSuccess = NodeRegistrationResponseStatus.Failure
                },
                null);
        }
        
        // Return the registration response - success
        return (
            new NodeRegistrationResponse
            {
                RegistrationSuccess = NodeRegistrationResponseStatus.Success
            },
            registeredNode);
    }

    // Get a registered node
    public RegisteredNode? GetRegisteredNode(Guid nodeGuid)
    {
        return _registeredNodes.Values.FirstOrDefault(node => node.NodeGuid == nodeGuid);
    }

    public async Task InvokeServiceChainAsync(
        ServiceChainRequest serviceChainRequest,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ResolvedServiceChainStep> resolvedSteps =
            ValidateAndResolveSteps(serviceChainRequest);
        var results = new Dictionary<string, JsonObject>(StringComparer.Ordinal);

        foreach (ResolvedServiceChainStep resolvedStep in resolvedSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ServiceChainStep step = resolvedStep.Step;
            ServiceRequest request = step.ServiceRequest;
            JsonObject resolvedParameters;
            try
            {
                resolvedParameters = ResolveParameters(request.Parameters, results);
            }
            catch (ServiceChainInvocationException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is InvalidOperationException
                    or KeyNotFoundException)
            {
                throw Failure(
                    StatusCodes.Status400BadRequest,
                    "result_reference_invalid",
                    "A result reference could not be resolved.",
                    step,
                    exception);
            }

            var resolvedRequest = new ServiceRequest
            {
                TargetNodeName = request.TargetNodeName,
                TargetServiceName = request.TargetServiceName,
                Parameters = resolvedParameters
            };

            ForwardedServiceResponse forwardedResponse;
            try
            {
                forwardedResponse = await _serviceRequestForwarder.ForwardAsync(
                    resolvedStep.InvokeUri,
                    resolvedRequest,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is HttpRequestException
                    or JsonException
                    or NotSupportedException
                    or InvalidDataException
                    or TaskCanceledException)
            {
                throw Failure(
                    StatusCodes.Status502BadGateway,
                    "service_forwarding_failed",
                    "The target node did not return a usable service response.",
                    step,
                    exception);
            }

            ServiceResponse serviceResponse = forwardedResponse.ServiceResponse;
            if (serviceResponse.Status != Navi_Protocol.TaskStatus.Success)
            {
                throw Failure(
                    StatusCodes.Status422UnprocessableEntity,
                    "service_invocation_failed",
                    "The target service did not complete successfully.",
                    step);
            }

            if (!IsSuccessfulStatusCode(forwardedResponse.HttpStatusCode))
            {
                throw Failure(
                    StatusCodes.Status502BadGateway,
                    "service_forwarding_failed",
                    "The target node returned an unsuccessful HTTP response.",
                    step);
            }

            if (serviceResponse.ReturnValues is null)
            {
                throw Failure(
                    StatusCodes.Status502BadGateway,
                    "service_forwarding_failed",
                    "The target node returned a malformed service response.",
                    step);
            }

            if (step.CallKey is not null)
            {
                results.Add(
                    step.CallKey,
                    (JsonObject)serviceResponse.ReturnValues.DeepClone());
            }
        }
    }

    private IReadOnlyList<ResolvedServiceChainStep> ValidateAndResolveSteps(
        ServiceChainRequest serviceChainRequest)
    {
        if (serviceChainRequest.Steps is null ||
            serviceChainRequest.Steps.Count == 0)
        {
            throw new ServiceChainInvocationException(
                StatusCodes.Status400BadRequest,
                "service_chain_invalid",
                "A service chain must contain at least one step.");
        }

        var callKeys = new HashSet<string>(StringComparer.Ordinal);
        var resolvedSteps =
            new List<ResolvedServiceChainStep>(serviceChainRequest.Steps.Count);

        foreach (ServiceChainStep? step in serviceChainRequest.Steps)
        {
            ValidateStep(step, callKeys);

            ServiceRequest request = step!.ServiceRequest;
            if (!_registeredNodes.TryGetValue(
                    request.TargetNodeName,
                    out RegisteredNode? targetNode))
            {
                throw Failure(
                    StatusCodes.Status404NotFound,
                    "target_node_not_found",
                    "The target node is not registered.",
                    step);
            }

            Uri invokeUri = new(
                $"{targetNode.NodeAddress.AbsoluteUri.TrimEnd('/')}/invoke",
                UriKind.Absolute);
            resolvedSteps.Add(new ResolvedServiceChainStep(step, invokeUri));

            if (step.CallKey is not null)
            {
                callKeys.Add(step.CallKey);
            }
        }

        return resolvedSteps;
    }

    private static void ValidateStep(
        ServiceChainStep? step,
        IReadOnlySet<string> availableCallKeys)
    {
        if (step?.ServiceRequest is null)
        {
            throw new ServiceChainInvocationException(
                StatusCodes.Status400BadRequest,
                "service_chain_invalid",
                "Every service chain step must contain a service request.");
        }

        ServiceRequest request = step.ServiceRequest;
        if (string.IsNullOrWhiteSpace(request.TargetNodeName) ||
            string.IsNullOrWhiteSpace(request.TargetServiceName) ||
            request.Parameters is null)
        {
            throw Failure(
                StatusCodes.Status400BadRequest,
                "service_request_invalid",
                "Every service request must identify a target node, target service, and parameters.",
                step);
        }

        if (step.CallKey is not null)
        {
            if (string.IsNullOrWhiteSpace(step.CallKey))
            {
                throw Failure(
                    StatusCodes.Status400BadRequest,
                    "call_key_invalid",
                    "A supplied call key cannot be empty or whitespace.",
                    step);
            }

            if (availableCallKeys.Contains(step.CallKey))
            {
                throw Failure(
                    StatusCodes.Status400BadRequest,
                    "call_key_duplicate",
                    "Call keys must be unique within a service chain.",
                    step);
            }
        }

        ValidateResultReferences(
            request.Parameters,
            availableCallKeys,
            step);
    }

    private static void ValidateResultReferences(
        JsonNode? node,
        IReadOnlySet<string> availableCallKeys,
        ServiceChainStep step)
    {
        if (node is JsonArray array)
        {
            foreach (JsonNode? item in array)
            {
                ValidateResultReferences(item, availableCallKeys, step);
            }

            return;
        }

        if (node is not JsonObject jsonObject)
        {
            return;
        }

        if (jsonObject.ContainsKey(ResultReferencePropertyName))
        {
            ResultReference reference = ParseResultReference(jsonObject, step);
            if (!availableCallKeys.Contains(reference.CallKey))
            {
                throw Failure(
                    StatusCodes.Status400BadRequest,
                    "result_reference_unavailable",
                    "A result reference must identify a previously completed keyed step.",
                    step);
            }

            return;
        }

        foreach ((_, JsonNode? value) in jsonObject)
        {
            ValidateResultReferences(value, availableCallKeys, step);
        }
    }

    private static JsonObject ResolveParameters(
        JsonObject parameters,
        IReadOnlyDictionary<string, JsonObject> results)
    {
        JsonNode? resolved = ResolveNode(parameters, results, step: null);
        return resolved as JsonObject
               ?? throw new InvalidOperationException(
                   "Service request parameters must resolve to a JSON object.");
    }

    private static JsonNode? ResolveNode(
        JsonNode? node,
        IReadOnlyDictionary<string, JsonObject> results,
        ServiceChainStep? step)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonArray array)
        {
            var resolvedArray = new JsonArray();
            foreach (JsonNode? item in array)
            {
                resolvedArray.Add(ResolveNode(item, results, step));
            }

            return resolvedArray;
        }

        if (node is not JsonObject jsonObject)
        {
            return node.DeepClone();
        }

        if (jsonObject.ContainsKey(ResultReferencePropertyName))
        {
            ResultReference reference = ParseResultReference(
                jsonObject,
                step);
            if (!results.TryGetValue(reference.CallKey, out JsonObject? returnValues))
            {
                throw new KeyNotFoundException(
                    "The referenced call key has no stored result.");
            }

            if (!returnValues.TryGetPropertyValue(
                    reference.ReturnValue,
                    out JsonNode? returnValue))
            {
                throw new KeyNotFoundException(
                    "The referenced return value does not exist.");
            }

            return returnValue?.DeepClone();
        }

        var resolvedObject = new JsonObject();
        foreach ((string propertyName, JsonNode? value) in jsonObject)
        {
            resolvedObject[propertyName] = ResolveNode(value, results, step);
        }

        return resolvedObject;
    }

    private static ResultReference ParseResultReference(
        JsonObject referenceContainer,
        ServiceChainStep? step)
    {
        string? callKey = null;
        string? returnValue = null;
        bool isCompleteReference =
            referenceContainer.Count == 1 &&
            referenceContainer[ResultReferencePropertyName] is JsonObject reference &&
            reference.Count == 2 &&
            reference[CallKeyPropertyName] is JsonValue callKeyValue &&
            callKeyValue.TryGetValue(out callKey) &&
            !string.IsNullOrWhiteSpace(callKey) &&
            reference[ReturnValuePropertyName] is JsonValue returnValueValue &&
            returnValueValue.TryGetValue(out returnValue) &&
            !string.IsNullOrWhiteSpace(returnValue);

        if (!isCompleteReference)
        {
            if (step is null)
            {
                throw new InvalidOperationException(
                    "The result reference is malformed.");
            }

            throw Failure(
                StatusCodes.Status400BadRequest,
                "result_reference_invalid",
                "A result reference must contain only non-empty call_key and return_value strings.",
                step);
        }

        return new ResultReference(callKey!, returnValue!);
    }

    private static bool IsSuccessfulStatusCode(HttpStatusCode statusCode)
    {
        int numericStatusCode = (int)statusCode;
        return numericStatusCode is >= 200 and <= 299;
    }

    private static ServiceChainInvocationException Failure(
        int statusCode,
        string code,
        string message,
        ServiceChainStep step,
        Exception? innerException = null)
    {
        return new ServiceChainInvocationException(
            statusCode,
            code,
            message,
            step.CallKey,
            step.ServiceRequest.TargetNodeName,
            step.ServiceRequest.TargetServiceName,
            innerException);
    }

    public void Dispose()
    {
        _serviceRequestForwarder.Dispose();
    }

    private sealed record ResolvedServiceChainStep(
        ServiceChainStep Step,
        Uri InvokeUri);

    private sealed record ResultReference(
        string CallKey,
        string ReturnValue);
}
