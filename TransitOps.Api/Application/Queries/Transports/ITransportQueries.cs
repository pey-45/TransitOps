using TransitOps.Api.Contracts.Responses.Transports;

namespace TransitOps.Api.Application.Queries.Transports;

public interface ITransportQueries
{
    Task<IReadOnlyList<TransportSummaryResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<TransportDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
