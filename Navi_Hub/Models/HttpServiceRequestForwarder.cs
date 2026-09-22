using System.Net;
using System.Net.Http.Json;
using Navi_Protocol;

namespace Navi_Hub.Models;

public sealed class HttpServiceRequestForwarder : IDisposable
{
    private readonly HttpClient _httpClient;

    public HttpServiceRequestForwarder()
        : this(new HttpClient())
    {
    }

    internal HttpServiceRequestForwarder(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ForwardedServiceResponse> ForwardAsync(
        Uri invokeUri,
        ServiceRequest serviceRequest,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            invokeUri,
            serviceRequest,
            cancellationToken);

        ServiceResponse? serviceResponse =
            await response.Content.ReadFromJsonAsync<ServiceResponse>(
                cancellationToken: cancellationToken);

        if (serviceResponse is null)
        {
            throw new InvalidDataException(
                "The target node returned an empty service response.");
        }

        return new ForwardedServiceResponse(
            response.StatusCode,
            serviceResponse);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public sealed record ForwardedServiceResponse(
    HttpStatusCode HttpStatusCode,
    ServiceResponse ServiceResponse);
