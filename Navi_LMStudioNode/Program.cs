using Navi_LMStudioNode;
using Navi_Protocol;
using Navi_ServiceSdk;

var builder = WebApplication.CreateBuilder(args);

LmStudioNodeConfiguration.Configure(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    builder.Environment.EnvironmentName,
    args);

Uri nodeAddress = LmStudioNodeConfiguration.GetNodeAddress(builder.Configuration);
builder.WebHost.UseUrls(nodeAddress.AbsoluteUri.TrimEnd('/'));

LmStudioOptions options = LmStudioOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(options);
builder.Services.AddHttpClient<LmStudioClient>();
builder.Services.AddSingleton<LmStudioNode>();
builder.Services.AddHostedService<LmStudioStartupHostedService>();

builder.Services.AddHttpClient();
builder.Services.AddHostedService<NodeRegistrationHostedService>();

var app = builder.Build();

app.MapGet("/", () => "Navi LM Studio Node");
app.MapPost(
    "/invoke",
    async (ServiceRequest request, LmStudioNode node) =>
    {
        Service? service = node.Services.SingleOrDefault(candidate =>
            string.Equals(
                candidate.ServiceDescription.ServiceName,
                request.TargetServiceName,
                StringComparison.Ordinal));

        if (service is null)
        {
            var failureResponse = new ServiceResponse
            {
                Status = Navi_Protocol.TaskStatus.Failed,
                Errors =
                [
                    new ServiceError
                    {
                        Code = "service_not_found",
                        Message = $"The service '{request.TargetServiceName}' is not hosted by this node.",
                        Recoverable = false
                    }
                ]
            };

            return Results.NotFound(failureResponse);
        }

        ServiceResponse response = await service.RunAsync(request);
        return Results.Ok(response);
    });

app.Run();

public partial class Program;
