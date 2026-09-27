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

public class RsvpFunctions
{
    private readonly ILogger<RsvpFunctions> _logger;
    private readonly IHouseholdRepository _repository;

    public RsvpFunctions(ILogger<RsvpFunctions> logger, IHouseholdRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("GetHouseholdRsvp")]
    public async Task<HttpResponseData> GetRsvp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.RsvpMyHousehold)] HttpRequestData request)
    {
        _logger.LogInformation("Get household RSVP endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.TryGetHouseholdId(request, out var householdId))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Authentication is required to access RSVP data."));
            return response;
        }

        var account = (await _repository.ListAsync()).FirstOrDefault(x => x.Id == householdId);
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
            account.Rsvp,
            Adults = account.Adults,
            Children = account.Children
        }, "RSVP retrieved successfully."));
        return response;
    }

    [Function("SaveRsvp")]
    public async Task<HttpResponseData> SaveRsvp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.RsvpSave)] HttpRequestData request)
    {
        _logger.LogInformation("Save RSVP endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        try
        {
            if (!AuthorizationContext.TryGetHouseholdId(request, out var authorizedHouseholdId))
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Authentication is required to save RSVP data."));
                return response;
            }

            var body = await request.ReadAsStringAsync() ?? string.Empty;
            var payload = JsonSerializer.Deserialize<RsvpSaveRequest>(body, JsonOptions.Default);

            if (payload is null || string.IsNullOrWhiteSpace(payload.HouseholdId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("HouseholdId is required."));
                return response;
            }

            if (payload.HouseholdId != authorizedHouseholdId)
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("You cannot update another household RSVP."));
                return response;
            }

            var account = (await _repository.ListAsync()).FirstOrDefault(x => x.Id == payload.HouseholdId);
            if (account is null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household not found."));
                return response;
            }

            foreach (var item in payload.Rsvp)
            {
                account.Rsvp[item.Key] = item.Value;
            }

            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _repository.SaveAsync(account);

            await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { account.Id, account.Rsvp }, "RSVP saved successfully."));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving household RSVP");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred while saving the RSVP."));
            return response;
        }
    }

    [Function("PatchRsvpStatus")]
    public async Task<HttpResponseData> PatchStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = ApiRoutes.RsvpSave)] HttpRequestData request)
    {
        _logger.LogInformation("Patch RSVP status endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        try
        {
            if (!AuthorizationContext.TryGetHouseholdId(request, out var authorizedHouseholdId))
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Authentication is required to update RSVP status."));
                return response;
            }

            var body = await request.ReadAsStringAsync() ?? string.Empty;
            var payload = JsonSerializer.Deserialize<RsvpStatusPatchRequest>(body, JsonOptions.Default);

            if (payload is null || string.IsNullOrWhiteSpace(payload.HouseholdId) || string.IsNullOrWhiteSpace(payload.GuestId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("HouseholdId and GuestId are required."));
                return response;
            }

            if (payload.HouseholdId != authorizedHouseholdId)
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("You cannot update another household RSVP status."));
                return response;
            }

            var account = (await _repository.ListAsync()).FirstOrDefault(x => x.Id == payload.HouseholdId);
            if (account is null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household not found."));
                return response;
            }

            var guest = account.Adults.FirstOrDefault(x => x.Id == payload.GuestId)
                ?? account.Children.FirstOrDefault(x => x.Id == payload.GuestId);

            if (guest is null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Guest not found."));
                return response;
            }

            guest.Attendance = payload.Status;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _repository.SaveAsync(account);

            await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { guest.Id, guest.Attendance }, "RSVP status updated successfully."));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching RSVP status");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred while updating RSVP status."));
            return response;
        }
    }

    [Function("CreateGuest")]
    public async Task<HttpResponseData> CreateGuest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.RsvpGuest)] HttpRequestData request)
    {
        _logger.LogInformation("Create guest endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        try
        {
            var body = await request.ReadAsStringAsync() ?? string.Empty;
            var payload = JsonSerializer.Deserialize<CreateGuestRequest>(body, JsonOptions.Default);

            if (payload is null || string.IsNullOrWhiteSpace(payload.HouseholdId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("HouseholdId is required."));
                return response;
            }

            var account = (await _repository.ListAsync()).FirstOrDefault(x => x.Id == payload.HouseholdId);
            if (account is null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household not found."));
                return response;
            }

            var guest = new HouseholdGuest
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = payload.Type,
                FirstName = payload.FirstName,
                LastName = payload.LastName,
                Attendance = payload.Attendance ?? "NOT_ANSWERED"
            };

            if (payload.Type == "CHILD")
            {
                account.Children.Add(guest);
            }
            else
            {
                account.Adults.Add(guest);
            }

            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _repository.SaveAsync(account);

            await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { guest.Id, guest.Type }, "Guest created successfully."));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred while creating the guest."));
            return response;
        }
    }

    [Function("DeleteGuest")]
    public async Task<HttpResponseData> DeleteGuest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.RsvpGuest + "/{guestId}")] HttpRequestData request)
    {
        _logger.LogInformation("Delete guest endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);
        var householdId = request.Query["householdId"];
        var guestId = request.Url?.Segments.LastOrDefault()?.Trim('/');

        if (string.IsNullOrWhiteSpace(householdId) || string.IsNullOrWhiteSpace(guestId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("householdId query parameter and guestId route parameter are required."));
            return response;
        }

        var account = (await _repository.ListAsync()).FirstOrDefault(x => x.Id == householdId);
        if (account is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household not found."));
            return response;
        }

        account.Adults.RemoveAll(x => x.Id == guestId);
        account.Children.RemoveAll(x => x.Id == guestId);
        account.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(account);

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { deleted = true }, "Guest deleted successfully."));
        return response;
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public sealed record RsvpSaveRequest(string HouseholdId, Dictionary<string, string> Rsvp);
    public sealed record RsvpStatusPatchRequest(string HouseholdId, string GuestId, string Status);
    public sealed record CreateGuestRequest(string HouseholdId, string Type, string FirstName, string LastName, string? Attendance = null);
}
