using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WeddingApi.Functions.Models;
using WeddingApi.Functions.Services;
using WeddingApi.Shared.Constants;
using WeddingApi.Shared.DTOs;

namespace WeddingApi.Functions.Functions;

public class HouseholdFunctions
{
    private readonly ILogger<HouseholdFunctions> _logger;
    private readonly IHouseholdRepository _repository;

    public HouseholdFunctions(ILogger<HouseholdFunctions> logger, IHouseholdRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("GetHouseholdProfile")]
    public async Task<HttpResponseData> GetProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.HouseholdMe)] HttpRequestData request)
    {
        _logger.LogInformation("Household profile endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.TryGetHouseholdId(request, out var householdId))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Authentication is required to access this household profile."));
            return response;
        }

        var accounts = await _repository.ListAsync();
        var account = accounts.FirstOrDefault(x => x.Id == householdId);
        if (account is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household not found."));
            return response;
        }

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new
        {
            account.Id,
            account.HouseholdName,
            account.Email,
            account.Adults,
            account.Children,
            account.Rsvp
        }, "Household profile retrieved successfully."));
        return response;
    }

    [Function("PatchHouseholdProfile")]
    public async Task<HttpResponseData> PatchProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = ApiRoutes.HouseholdProfile)] HttpRequestData request)
    {
        _logger.LogInformation("Patch household profile endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        try
        {
            if (!AuthorizationContext.TryGetHouseholdId(request, out var authorizedHouseholdId))
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Authentication is required to update this household profile."));
                return response;
            }

            var body = await request.ReadAsStringAsync() ?? string.Empty;
            var payload = JsonSerializer.Deserialize<HouseholdProfilePatchRequest>(body, JsonOptions.Default);

            if (payload is null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household update data is required."));
                return response;
            }

            if (!string.IsNullOrWhiteSpace(payload.HouseholdId) && payload.HouseholdId != authorizedHouseholdId)
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("You cannot update another household profile."));
                return response;
            }

            var account = (await _repository.ListAsync()).FirstOrDefault(x => x.Id == authorizedHouseholdId);
            if (account is null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household not found."));
                return response;
            }

            if (!string.IsNullOrWhiteSpace(payload.HouseholdName))
            {
                account.HouseholdName = payload.HouseholdName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(payload.Email))
            {
                account.Email = payload.Email.Trim();
            }

            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _repository.SaveAsync(account);

            await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { account.Id, account.HouseholdName, account.Email }, "Household profile updated successfully."));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching household profile");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred while updating the household profile."));
            return response;
        }
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public sealed record HouseholdProfilePatchRequest(string HouseholdId, string? HouseholdName, string? Email);
}
