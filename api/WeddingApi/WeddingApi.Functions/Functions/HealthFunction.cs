using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using WeddingApi.Shared.Constants;
using WeddingApi.Shared.DTOs;

namespace WeddingApi.Functions.Functions;

public class HealthFunction
{
    private readonly ILogger<HealthFunction> _logger;

    public HealthFunction(ILogger<HealthFunction> logger)
    {
        _logger = logger;
    }

    [Function(ApiRoutes.Health)]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Health)] HttpRequestData request)
    {
        _logger.LogInformation("Health endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.WriteAsJsonAsync(ApiResponse<string>.Ok("healthy", "Wedding API is running."));
        return response;
    }
}
