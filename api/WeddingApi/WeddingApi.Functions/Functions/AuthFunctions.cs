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

public class AuthFunctions
{
    private readonly ILogger<AuthFunctions> _logger;
    private readonly IHouseholdRepository _repository;
    private readonly PasswordHasher _passwordHasher = new();

    public AuthFunctions(ILogger<AuthFunctions> logger, IHouseholdRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [Function("RegisterHousehold")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.AuthRegister)] HttpRequestData request)
    {
        _logger.LogInformation("Register household endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        try
        {
            var body = await request.ReadAsStringAsync() ?? string.Empty;
            var payload = JsonSerializer.Deserialize<RegisterHouseholdRequest>(body, JsonOptions.Default);

            if (payload is null || string.IsNullOrWhiteSpace(payload.HouseholdName) || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Password))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Household name, email, and password are required."));
                return response;
            }

            if (payload.Password.Length < 8)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Password must contain at least 8 characters."));
                return response;
            }

            var existing = await _repository.GetByEmailAsync(payload.Email);
            if (existing is not null)
            {
                response.StatusCode = HttpStatusCode.Conflict;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("This email is already registered to an active household."));
                return response;
            }

            var passwordHash = _passwordHasher.HashPassword(payload.Password);
            var account = new HouseholdAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                HouseholdName = payload.HouseholdName.Trim(),
                Email = payload.Email.Trim(),
                PasswordHash = passwordHash.Hash,
                PasswordSalt = passwordHash.Salt,
                Adults = [ new HouseholdGuest { Id = Guid.NewGuid().ToString("N"), Type = "ADULT" } ],
                Children = [],
                Rsvp = new Dictionary<string, string>
                {
                    ["ceremony"] = "NOT_ANSWERED",
                    ["dinner"] = "NOT_ANSWERED",
                    ["brunch"] = "NOT_ANSWERED"
                }
            };

            await _repository.SaveAsync(account);
            await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { accountId = account.Id, householdName = account.HouseholdName }, "Household registration successful."));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering household");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred while registering the household."));
            return response;
        }
    }

    [Function("LoginHousehold")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.AuthLogin)] HttpRequestData request)
    {
        _logger.LogInformation("Login household endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);

        try
        {
            var body = await request.ReadAsStringAsync() ?? string.Empty;
            var payload = JsonSerializer.Deserialize<LoginHouseholdRequest>(body, JsonOptions.Default);

            if (payload is null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Password))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("Email and password are required."));
                return response;
            }

            var account = await _repository.GetByEmailAsync(payload.Email);
            if (account is null)
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("The email or password is incorrect."));
                return response;
            }

            var isValid = _passwordHasher.VerifyPassword(payload.Password, new PasswordHashResult(account.PasswordHash, account.PasswordSalt));
            if (!isValid)
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                await response.WriteAsJsonAsync(ApiResponse<object>.Fail("The email or password is incorrect."));
                return response;
            }

            await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { accountId = account.Id, householdName = account.HouseholdName }, "Welcome back."));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in household");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(ApiResponse<object>.Fail("An unexpected error occurred while logging in."));
            return response;
        }
    }

    [Function("LogoutHousehold")]
    public async Task<HttpResponseData> Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.AuthLogout)] HttpRequestData request)
    {
        _logger.LogInformation("Logout household endpoint called");

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(ApiResponse<object>.Ok(new { loggedOut = true }, "Household logout successful."));
        return response;
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public sealed record RegisterHouseholdRequest(string HouseholdName, string Email, string Password);
    public sealed record LoginHouseholdRequest(string Email, string Password);
}
