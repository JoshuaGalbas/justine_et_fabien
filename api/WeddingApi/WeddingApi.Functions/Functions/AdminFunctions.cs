using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WeddingApi.Functions.Services;
using WeddingApi.Shared.Constants;
using WeddingApi.Shared.DTOs;

namespace WeddingApi.Functions.Functions;

public class AdminFunctions
{
    private readonly ILogger<AdminFunctions> _logger;
    private readonly IHouseholdRepository _repository;

    public AdminFunctions(ILogger<AdminFunctions> logger, IHouseholdRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("GetAdminDashboard")]
    public async Task<HttpResponseData> GetDashboard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organizer/dashboard")] HttpRequestData request)
    {
        _logger.LogInformation("Admin dashboard endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.IsAdminRequest(request))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Admin authorization is required."));
            return response;
        }

        var households = await _repository.ListAsync();
        var totalGuests = households.Sum(x => x.Adults.Count + x.Children.Count);
        var confirmedGuests = households.Sum(x => x.Adults.Count(g => g.Attendance == "ATTENDING") + x.Children.Count(g => g.Attendance == "ATTENDING"));
        var respondedHouseholds = households.Count(x => x.Rsvp.Values.Any(v => v != "NOT_ANSWERED"));

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new
        {
            households = households.Count,
            guests = totalGuests,
            confirmedGuests,
            respondedHouseholds
        }, "Dashboard retrieved successfully."));
        return response;
    }

    [Function("GetAdminHouseholds")]
    public async Task<HttpResponseData> GetHouseholds(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organizer/households")] HttpRequestData request)
    {
        _logger.LogInformation("Admin households endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.IsAdminRequest(request))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Admin authorization is required."));
            return response;
        }

        var households = await _repository.ListAsync();

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new
        {
            households = households.Select(x => new
            {
                x.Id,
                x.HouseholdName,
                x.Email,
                x.Rsvp,
                adults = x.Adults.Count,
                children = x.Children.Count,
                updatedAt = x.UpdatedAt
            })
        }, "Households retrieved successfully."));
        return response;
    }

    [Function("PatchHouseholdStatus")]
    public async Task<HttpResponseData> PatchHouseholdStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "organizer/household/{householdId}/status")] HttpRequestData request)
    {
        _logger.LogInformation("Patch household status endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.IsAdminRequest(request))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Admin authorization is required."));
            return response;
        }

        var householdId = request.Url?.Segments.LastOrDefault()?.Trim('/');

        if (string.IsNullOrWhiteSpace(householdId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("householdId is required."));
            return response;
        }

        var body = await request.ReadAsStringAsync() ?? string.Empty;
        var payload = string.IsNullOrWhiteSpace(body)
            ? new ConfirmationRequest(false)
            : JsonSerializer.Deserialize<ConfirmationRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var confirmed = payload is not null && payload.Confirm;

        if (!confirmed)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Confirmation is required before changing a household status."));
            return response;
        }

        var household = (await _repository.ListAsync()).FirstOrDefault(x => x.Id == householdId);
        if (household is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household not found."));
            return response;
        }

        household.Rsvp["status"] = "CONFIRMED";
        household.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(household);
        await _repository.RecordAuditAsync(new AuditLogEntry
        {
            Action = "status_confirmed",
            HouseholdId = household.Id,
            Actor = "admin",
            Details = new Dictionary<string, string> { ["status"] = household.Rsvp["status"] }
        });

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { household.Id, status = household.Rsvp["status"] }, "Household status updated successfully."));
        return response;
    }

    [Function("GetAuditLog")]
    public async Task<HttpResponseData> GetAuditLog(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organizer/audit-log")] HttpRequestData request)
    {
        _logger.LogInformation("Audit log endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.IsAdminRequest(request))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Admin authorization is required."));
            return response;
        }

        var auditEntries = await _repository.ListAuditEntriesAsync();
        var entries = auditEntries.Select(x => new
        {
            x.Id,
            x.Action,
            x.HouseholdId,
            x.Actor,
            x.PerformedAt,
            x.Details
        }).ToList();

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { entries }, "Audit log retrieved successfully."));
        return response;
    }

    [Function("UploadGalleryPhoto")]
    public async Task<HttpResponseData> UploadPhoto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "organizer/gallery/upload")] HttpRequestData request)
    {
        _logger.LogInformation("Gallery upload endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.IsAdminRequest(request))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Admin authorization is required."));
            return response;
        }

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { uploaded = true, approved = false }, "Gallery photo uploaded successfully."));
        return response;
    }

    [Function("ApproveGalleryPhoto")]
    public async Task<HttpResponseData> ApprovePhoto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "organizer/gallery/approve/{photoId}")] HttpRequestData request)
    {
        _logger.LogInformation("Gallery approval endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.IsAdminRequest(request))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Admin authorization is required."));
            return response;
        }

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { approved = true }, "Gallery photo approved successfully."));
        return response;
    }

    [Function("DeleteGalleryPhoto")]
    public async Task<HttpResponseData> DeletePhoto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "organizer/gallery/{photoId}")] HttpRequestData request)
    {
        _logger.LogInformation("Gallery delete endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        if (!AuthorizationContext.IsAdminRequest(request))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Admin authorization is required."));
            return response;
        }

        var body = await request.ReadAsStringAsync() ?? string.Empty;
        var confirmed = body.Contains("\"confirm\"", StringComparison.OrdinalIgnoreCase)
            && body.Contains("true", StringComparison.OrdinalIgnoreCase);

        if (!confirmed)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Confirmation is required before deleting a gallery photo."));
            return response;
        }

        await _repository.RecordAuditAsync(new AuditLogEntry
        {
            Action = "photo_deleted",
            HouseholdId = "admin",
            Actor = "admin",
            Details = new Dictionary<string, string> { ["photoId"] = request.Url?.Segments.LastOrDefault()?.Trim('/') ?? "unknown" }
        });

        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { deleted = true }, "Gallery photo deleted successfully."));
        return response;
    }

    private sealed record ConfirmationRequest(bool Confirm);
}
