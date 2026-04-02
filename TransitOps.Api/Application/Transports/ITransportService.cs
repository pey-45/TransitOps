using TransitOps.Api.Contracts.Requests.Transports;
using TransitOps.Api.Contracts.Responses.Transports;

namespace TransitOps.Api.Application.Transports;

public interface ITransportService
{
    Task<IReadOnlyList<TransportSummaryResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<TransportDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TransportDetailResponse> CreateAsync(
        UpsertTransportRequest request,
        CancellationToken cancellationToken);

    Task<TransportDetailResponse> UpdateAsync(
        Guid id,
        UpsertTransportRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
