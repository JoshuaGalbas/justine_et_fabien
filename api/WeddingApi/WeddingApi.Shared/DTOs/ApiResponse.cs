namespace WeddingApi.Shared.DTOs;

public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data = default,
    string? ErrorCode = null,
    DateTimeOffset? Timestamp = null
)
{
    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new(true, message, data, null, DateTimeOffset.UtcNow);

    public static ApiResponse<T> Fail(string message, string? errorCode = null) =>
        new(false, message, default, errorCode, DateTimeOffset.UtcNow);
}
