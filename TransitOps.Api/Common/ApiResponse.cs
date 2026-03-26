namespace TransitOps.Api.Common;

public sealed record ApiResponse<T>(T Data, ApiResponseMetadata Meta)
{
    public static ApiResponse<T> Success(T data, string requestId)
    {
        return new ApiResponse<T>(data, ApiResponseMetadata.Create(requestId));
    }
}
