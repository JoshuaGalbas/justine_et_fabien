using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using WeddingApi.Shared.Constants;
using WeddingApi.Shared.DTOs;

namespace WeddingApi.Functions.Functions;

public class PublicContentFunction
{
    private readonly ILogger<PublicContentFunction> _logger;

    public PublicContentFunction(ILogger<PublicContentFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetPublicContent")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.PublicContent)] HttpRequestData request)
    {
        _logger.LogInformation("Public content endpoint called");

        var payload = new
        {
            title = "Justine & Fabien",
            subtitle = "We are getting married",
            locale = "en",
            hero = new
            {
                headlineEn = "We are getting married",
                headlineFr = "Nous nous marions",
                ctaEn = "Join our celebration",
                ctaFr = "Rejoignez notre célébration"
            }
        };

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.WriteAsJsonAsync(ApiResponse<object>.Ok(payload, "Public content retrieved successfully."));
        return response;
    }
}
