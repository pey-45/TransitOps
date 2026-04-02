using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Transports;
using TransitOps.Api.Contracts.Responses.Transports;

namespace TransitOps.Api.Application.Transports;

public interface ITransportService
{
    Task<PagedResult<TransportSummaryResponse>> GetAllAsync(
        GetTransportsRequest request,
        CancellationToken cancellationToken);

    Task<TransportDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TransportDetailResponse> CreateAsync(
        UpsertTransportRequest request,
        CancellationToken cancellationToken);

    Task<TransportDetailResponse> UpdateAsync(
        Guid id,
        UpsertTransportRequest request,
        CancellationToken cancellationToken);

    Task<TransportDetailResponse> AssignAsync(
        Guid id,
        AssignTransportRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
