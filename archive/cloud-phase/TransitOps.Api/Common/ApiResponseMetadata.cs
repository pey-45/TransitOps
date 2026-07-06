namespace TransitOps.Api.Common;

public sealed record ApiResponseMetadata(
    string RequestId,
    DateTime TimestampUtc,
    ApiPaginationMetadata? Pagination)
{
    public static ApiResponseMetadata Create(
        string requestId,
        ApiPaginationMetadata? pagination = null)
    {
        return new ApiResponseMetadata(requestId, DateTime.UtcNow, pagination);
    }
}
