namespace TransitOps.Api.Common;

public sealed record ApiResponse<T>(T Data, string RequestId)
{
    public static ApiResponse<T> Success(T data, string requestId) => new(data, requestId);
}

public sealed record ApiError(string Code, string Message, object? Details = null);

public sealed record ApiErrorResponse(ApiError Error, string RequestId)
{
    public static ApiErrorResponse Create(string code, string message, string requestId, object? details = null) =>
        new(new ApiError(code, message, details), requestId);
}
