namespace TransitOps.Api.Common;

public sealed record ApiResponseMetadata(string RequestId, DateTime TimestampUtc)
{
    public static ApiResponseMetadata Create(string requestId)
    {
        return new ApiResponseMetadata(requestId, DateTime.UtcNow);
    }
}
