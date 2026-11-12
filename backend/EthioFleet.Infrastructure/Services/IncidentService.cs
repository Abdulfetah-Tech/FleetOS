using EthioFleet.Application.Common;
using EthioFleet.Application.Incidents;
using EthioFleet.Domain.Incidents;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class IncidentService(AppDbContext dbContext, EthioFleet.Application.Interfaces.IClock clock) : IIncidentService
{
    public async Task<IncidentResult> CreateAsync(CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return IncidentResult.Failure("Vehicle was not found.", notFound: true);
        }
        var driver = request.DriverId is Guid driverId
            ? await dbContext.Drivers.SingleOrDefaultAsync(item => item.Id == driverId, cancellationToken)
            : null;
        if (request.DriverId.HasValue && driver is null)
        {
            return IncidentResult.Failure("Driver was not found.", notFound: true);
        }

        var incident = new Incident(vehicle.OrganizationId, vehicle, driver, request.Type, request.Severity,
            request.OccurredAt, request.Location, request.Description, request.EstimatedCost, request.CurrencyCode);
        dbContext.Incidents.Add(incident);
        await dbContext.SaveChangesAsync(cancellationToken);
        return IncidentResult.Success(ToResponse(incident));
    }

    public async Task<IncidentResult> GetAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        var incident = await Query().SingleOrDefaultAsync(item => item.Id == incidentId, cancellationToken);
        return incident is null
            ? IncidentResult.Failure("Incident was not found.", notFound: true)
            : IncidentResult.Success(ToResponse(incident));
    }

    public async Task<IncidentPage> ListAsync(
        Guid? vehicleId, IncidentStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var window = PageWindow.Create(page, pageSize);
        IQueryable<Incident> incidents = Query();
        if (vehicleId is Guid requestedVehicle)
        {
            incidents = incidents.Where(item => item.VehicleId == requestedVehicle);
        }
        if (status is IncidentStatus requestedStatus)
        {
            incidents = incidents.Where(item => item.Status == requestedStatus);
        }

        var total = await incidents.CountAsync(cancellationToken);
        var items = await incidents.OrderByDescending(item => item.OccurredAt)
            .ThenBy(item => item.Id)
            .Skip(window.Offset)
            .Take(window.PageSize)
            .ToListAsync(cancellationToken);
        return new IncidentPage(items.Select(ToResponse).ToArray(), window.Page, window.PageSize, total);
    }

    public async Task<IncidentResult> UpdateStatusAsync(
        Guid incidentId, UpdateIncidentStatusRequest request, CancellationToken cancellationToken)
    {
        var incident = await Query().SingleOrDefaultAsync(item => item.Id == incidentId, cancellationToken);
        if (incident is null)
        {
            return IncidentResult.Failure("Incident was not found.", notFound: true);
        }

        incident.UpdateStatus(request.Status, request.ResolutionNotes, request.ActualCost, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return IncidentResult.Success(ToResponse(incident));
    }

    private IQueryable<Incident> Query() => dbContext.Incidents
        .Include(item => item.Vehicle)
        .Include(item => item.Driver);

    private static IncidentResponse ToResponse(Incident incident) => new(
        incident.Id,
        incident.OrganizationId,
        incident.VehicleId,
        incident.Vehicle.PlateNumber,
        incident.DriverId,
        incident.Driver?.FullName,
        incident.Type,
        incident.Severity,
        incident.OccurredAt,
        incident.Location,
        incident.Description,
        incident.EstimatedCost,
        incident.ActualCost,
        incident.CurrencyCode,
        incident.Status,
        string.IsNullOrEmpty(incident.ResolutionNotes) ? null : incident.ResolutionNotes,
        incident.ResolvedAt);
}
