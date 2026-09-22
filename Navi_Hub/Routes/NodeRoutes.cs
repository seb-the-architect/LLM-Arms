using Navi_Hub.Models;
using Navi_Protocol;

namespace Navi_Hub.Routes;

public static class NodeRoutes
{
    public static IEndpointRouteBuilder MapNodeRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/nodes/register",
            (NodeRegistrationRequest request, Hub hub) =>
            {
                var registration = hub.RegisterNode(request);

                if (registration.RegisteredNode is null)
                {
                    return Results.Conflict(registration.Response);
                }

                return Results.Created(
                    $"/nodes/{registration.RegisteredNode.NodeGuid}",
                    registration.Response);
            });

        endpoints.MapGet(
            "/nodes",
            (Hub hub) => Results.Ok(hub.RegisteredNodes));

        endpoints.MapGet(
            "/nodes/{id:guid}",
            (Guid id, Hub hub) =>
            {
                RegisteredNode? registeredNode = hub.GetRegisteredNode(id);

                return registeredNode is null
                    ? Results.NotFound()
                    : Results.Ok(registeredNode);
            });

        endpoints.MapPost(
            "/nodes/service_chain_invoke",
            async (
                ServiceChainRequest request,
                Hub hub,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    await hub.InvokeServiceChainAsync(
                        request,
                        cancellationToken);
                    return Results.NoContent();
                }
                catch (ServiceChainInvocationException exception)
                {
                    var extensions = new Dictionary<string, object?>
                    {
                        ["code"] = exception.Code
                    };

                    if (exception.CallKey is not null)
                    {
                        extensions["call_key"] = exception.CallKey;
                    }

                    if (exception.TargetNodeName is not null)
                    {
                        extensions["target_node_name"] =
                            exception.TargetNodeName;
                    }

                    if (exception.TargetServiceName is not null)
                    {
                        extensions["target_service_name"] =
                            exception.TargetServiceName;
                    }

                    return Results.Problem(
                        statusCode: exception.StatusCode,
                        title: "Service chain invocation failed.",
                        detail: exception.Message,
                        extensions: extensions);
                }
            });

        return endpoints;
    }
}
