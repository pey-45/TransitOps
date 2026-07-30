using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Shipments;
using TransitOps.Api.Persistence;

namespace TransitOps.Api.Features.Reporting;

public sealed class SummaryService(TransitOpsDbContext dbContext) : ISummaryService
{
    private static readonly TimeSpan DefaultPeriod = TimeSpan.FromDays(30);

    public async Task<SummaryResponse> GetAsync(
        SummaryQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var to = ShipmentTime.Utc(query.To) ?? now;
        var from = ShipmentTime.Utc(query.From) ?? (query.To.HasValue ? to.Subtract(DefaultPeriod) : now.Subtract(DefaultPeriod));
        if (to < from)
            throw new ApiException(400, "summary_period_invalid", "El fin del rango no puede ser anterior al inicio.");

        var groupedStatuses = await dbContext.Shipments.AsNoTracking()
            .GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var counts = groupedStatuses.ToDictionary(item => item.Status, item => item.Count);
        var shipments = new ShipmentStatusCounts(
            Count(ShipmentStatus.Planned),
            Count(ShipmentStatus.InProgress),
            Count(ShipmentStatus.Delivered),
            Count(ShipmentStatus.Cancelled),
            groupedStatuses.Sum(item => item.Count));

        var vehicles = await VehicleActivity(from, to, cancellationToken);
        var drivers = await DriverActivity(from, to, cancellationToken);
        var incidents = await dbContext.ShipmentEvents.AsNoTracking().CountAsync(
            item => item.EventType == ShipmentEventType.Incident &&
                    item.OccurredAt >= from &&
                    item.OccurredAt <= to,
            cancellationToken);

        return new SummaryResponse(shipments, vehicles, drivers, incidents, from, to);

        int Count(ShipmentStatus status) => counts.GetValueOrDefault(status);
    }

    private async Task<IReadOnlyList<ResourceActivity>> VehicleActivity(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var activity = await dbContext.Shipments.AsNoTracking()
            .Where(item => item.PlannedPickupAt >= from &&
                           item.PlannedPickupAt <= to &&
                           item.VehicleId != null)
            .GroupBy(item => item.VehicleId!.Value)
            .Select(group => new { Id = group.Key, ShipmentCount = group.Count() })
            .ToListAsync(cancellationToken);
        var ids = activity.Select(item => item.Id).ToArray();
        var labels = await dbContext.Vehicles.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.LicensePlate, cancellationToken);

        return activity
            .Where(item => labels.ContainsKey(item.Id))
            .Select(item => new ResourceActivity(item.Id, labels[item.Id], item.ShipmentCount))
            .OrderByDescending(item => item.ShipmentCount)
            .ThenBy(item => item.Label)
            .ToList();
    }

    private async Task<IReadOnlyList<ResourceActivity>> DriverActivity(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var activity = await dbContext.Shipments.AsNoTracking()
            .Where(item => item.PlannedPickupAt >= from &&
                           item.PlannedPickupAt <= to &&
                           item.DriverId != null)
            .GroupBy(item => item.DriverId!.Value)
            .Select(group => new { Id = group.Key, ShipmentCount = group.Count() })
            .ToListAsync(cancellationToken);
        var ids = activity.Select(item => item.Id).ToArray();
        var labels = await dbContext.Drivers.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        return activity
            .Where(item => labels.ContainsKey(item.Id))
            .Select(item => new ResourceActivity(item.Id, labels[item.Id], item.ShipmentCount))
            .OrderByDescending(item => item.ShipmentCount)
            .ThenBy(item => item.Label)
            .ToList();
    }
}
