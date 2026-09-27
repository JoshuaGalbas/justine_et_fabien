namespace WeddingApi.Functions.Services;

using Microsoft.Azure.Functions.Worker.Http;

public static class AuthorizationContext
{
    public static bool TryGetHouseholdId(HttpRequestData request, out string householdId)
    {
        householdId = string.Empty;

        if (request.Headers.TryGetValues("x-household-id", out var householdIdValues))
        {
            var value = householdIdValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                householdId = value.Trim();
                return true;
            }
        }

        if (request.Headers.TryGetValues("Authorization", out var authorizationValues))
        {
            var rawValue = authorizationValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                var token = rawValue.Trim();
                const string bearerPrefix = "Bearer ";

                if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    token = token[bearerPrefix.Length..].Trim();
                }

                if (!string.IsNullOrWhiteSpace(token))
                {
                    householdId = token;
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsAdminRequest(HttpRequestData request)
    {
        var adminKey = Environment.GetEnvironmentVariable("WeddingAdminKey");
        if (string.IsNullOrWhiteSpace(adminKey))
        {
            return false;
        }

        if (request.Headers.TryGetValues("x-admin-key", out var headerValues))
        {
            var rawValue = headerValues.FirstOrDefault();
            if (string.Equals(rawValue, adminKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (request.Headers.TryGetValues("Authorization", out var authValues))
        {
            var rawValue = authValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            var token = rawValue.Trim();
            const string bearerPrefix = "Bearer ";
            if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                token = token[bearerPrefix.Length..].Trim();
            }

            return string.Equals(token, adminKey, StringComparison.Ordinal);
        }

        return false;
    }
}
