using Navi_Hub.Models;

namespace Navi_Hub.Routes;

public static class HealthRoutes
{
    public static IEndpointRouteBuilder MapHealthRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/health",
            (Hub _) => Results.Ok());

        return endpoints;
    }
}
