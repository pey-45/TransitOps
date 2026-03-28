using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Application.Queries.Transports;
using TransitOps.Api.Contracts.Responses.Transports;
using TransitOps.Api.Persistence;

namespace TransitOps.Api.Infrastructure.Queries.Transports;

public sealed class TransportQueries : ITransportQueries
{
    private readonly TransitOpsDbContext _dbContext;

    public TransportQueries(TransitOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TransportSummaryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Transports
            .AsNoTracking()
            .Where(transport => transport.DeletedAt == null)
            .OrderBy(transport => transport.PlannedPickupAt)
            .ThenBy(transport => transport.Reference)
            .Select(transport => new TransportSummaryResponse(
                transport.Id,
                transport.Reference,
                transport.Origin,
                transport.Destination,
                transport.Status,
                transport.VehicleId,
                transport.DriverId,
                transport.PlannedPickupAt,
                transport.PlannedDeliveryAt,
                transport.UpdatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TransportDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Transports
            .AsNoTracking()
            .Where(transport => transport.Id == id && transport.DeletedAt == null)
            .Select(transport => new TransportDetailResponse(
                transport.Id,
                transport.Reference,
                transport.Description,
                transport.Origin,
                transport.Destination,
                transport.PlannedPickupAt,
                transport.PlannedDeliveryAt,
                transport.ActualPickupAt,
                transport.ActualDeliveryAt,
                transport.Status,
                transport.VehicleId,
                transport.DriverId,
                transport.CreatedAt,
                transport.UpdatedAt,
                transport.DeletedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
