namespace TransitOps.Api.Common;

public sealed record ApiErrorResponse(ApiError Error, ApiResponseMetadata Meta)
{
    public static ApiErrorResponse Create(
        string code,
        string message,
        string requestId,
        IReadOnlyDictionary<string, string[]>? details = null)
    {
        return new ApiErrorResponse(
            new ApiError(code, message, details),
            ApiResponseMetadata.Create(requestId));
    }
}
