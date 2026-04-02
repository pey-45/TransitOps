using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Application.Transports;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Transports;
using TransitOps.Api.Contracts.Responses.Transports;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Errors;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Api.Infrastructure.Transports;

public sealed class TransportService : ITransportService
{
    private readonly TransitOpsDbContext _dbContext;

    public TransportService(TransitOpsDbContext dbContext)
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

    public async Task<TransportDetailResponse> CreateAsync(
        UpsertTransportRequest request,
        CancellationToken cancellationToken)
    {
        var reference = request.Reference.Trim();
        await EnsureReferenceIsUniqueAsync(reference, excludedTransportId: null, cancellationToken);

        var transport = new Transport
        {
            Reference = reference,
            Description = NormalizeOptionalText(request.Description),
            Origin = request.Origin.Trim(),
            Destination = request.Destination.Trim(),
            PlannedPickupAt = DateTimePersistence.AsUnspecified(request.PlannedPickupAt),
            PlannedDeliveryAt = DateTimePersistence.AsUnspecified(request.PlannedDeliveryAt)
        };

        _dbContext.Transports.Add(transport);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDetailResponse(transport);
    }

    public async Task<TransportDetailResponse> UpdateAsync(
        Guid id,
        UpsertTransportRequest request,
        CancellationToken cancellationToken)
    {
        var transport = await GetActiveTransportAsync(id, cancellationToken);

        var reference = request.Reference.Trim();
        await EnsureReferenceIsUniqueAsync(reference, transport.Id, cancellationToken);

        transport.Reference = reference;
        transport.Description = NormalizeOptionalText(request.Description);
        transport.Origin = request.Origin.Trim();
        transport.Destination = request.Destination.Trim();
        transport.PlannedPickupAt = DateTimePersistence.AsUnspecified(request.PlannedPickupAt);
        transport.PlannedDeliveryAt = DateTimePersistence.AsUnspecified(request.PlannedDeliveryAt);
        transport.UpdatedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDetailResponse(transport);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transport = await GetActiveTransportAsync(id, cancellationToken);
        var deletedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

        transport.DeletedAt = deletedAt;
        transport.UpdatedAt = deletedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureReferenceIsUniqueAsync(
        string reference,
        Guid? excludedTransportId,
        CancellationToken cancellationToken)
    {
        var existingTransportQuery = _dbContext.Transports
            .Where(transport => transport.DeletedAt == null && transport.Reference == reference);

        if (excludedTransportId.HasValue)
        {
            existingTransportQuery = existingTransportQuery
                .Where(transport => transport.Id != excludedTransportId.Value);
        }

        var referenceAlreadyExists = await existingTransportQuery.AnyAsync(cancellationToken);

        if (referenceAlreadyExists)
        {
            throw new ConflictException(
                "transport_reference_conflict",
                $"Transport reference '{reference}' already exists.");
        }
    }

    private async Task<Transport> GetActiveTransportAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transport = await _dbContext.Transports
            .SingleOrDefaultAsync(
                existingTransport => existingTransport.Id == id && existingTransport.DeletedAt == null,
                cancellationToken);

        if (transport is null)
        {
            throw new ResourceNotFoundException("transport_not_found", $"Transport '{id}' was not found.");
        }

        return transport;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static TransportDetailResponse MapToDetailResponse(Transport transport)
    {
        return new TransportDetailResponse(
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
            transport.DeletedAt);
    }
}
