namespace TransitOps.Api.Common;

public sealed record ApiPaginationMetadata(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
