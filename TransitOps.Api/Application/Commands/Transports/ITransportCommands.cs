using TransitOps.Api.Contracts.Requests.Transports;
using TransitOps.Api.Contracts.Responses.Transports;

namespace TransitOps.Api.Application.Commands.Transports;

public interface ITransportCommands
{
    Task<TransportDetailResponse> CreateAsync(
        UpsertTransportRequest request,
        CancellationToken cancellationToken);

    Task<TransportDetailResponse> UpdateAsync(
        Guid id,
        UpsertTransportRequest request,
        CancellationToken cancellationToken);
}
