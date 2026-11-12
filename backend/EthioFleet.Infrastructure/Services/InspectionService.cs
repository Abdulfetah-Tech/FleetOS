using EthioFleet.Application.Common;
using EthioFleet.Application.Inspections;
using EthioFleet.Domain.Inspections;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ApplicationInspectionResult = EthioFleet.Application.Inspections.InspectionResult;

namespace EthioFleet.Infrastructure.Services;

public sealed class InspectionService(
    AppDbContext dbContext,
    EthioFleet.Application.Interfaces.ICurrentUser currentUser,
    EthioFleet.Application.Interfaces.IClock clock) : IInspectionService
{
    public async Task<ApplicationInspectionResult> CreateAsync(CreateInspectionRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return ApplicationInspectionResult.Failure("Vehicle was not found.", notFound: true);
        }

        var inspection = new VehicleInspection(vehicle.OrganizationId, vehicle, clock.UtcNow, request.Odometer,
            currentUser.UserId, new InspectionChecklistDefinition(request.ChecklistVersion, request.ChecklistItems), request.Notes);
        dbContext.VehicleInspections.Add(inspection);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationInspectionResult.Success(ToResponse(inspection));
    }

    public async Task<ApplicationInspectionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var inspection = await Query().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return inspection is null
            ? ApplicationInspectionResult.Failure("Inspection was not found.", notFound: true)
            : ApplicationInspectionResult.Success(ToResponse(inspection));
    }

    public async Task<InspectionPage> ListAsync(Guid? vehicleId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var window = PageWindow.Create(page, pageSize);
        IQueryable<VehicleInspection> inspections = Query();
        if (vehicleId is Guid id)
        {
            inspections = inspections.Where(item => item.VehicleId == id);
        }

        var total = await inspections.CountAsync(cancellationToken);
        var items = await inspections.OrderByDescending(item => item.InspectedAt)
            .ThenBy(item => item.Id)
            .Skip(window.Offset)
            .Take(window.PageSize)
            .ToListAsync(cancellationToken);
        return new InspectionPage(items.Select(ToResponse).ToArray(), window.Page, window.PageSize, total);
    }

    public async Task<ApplicationInspectionResult> CompleteAsync(Guid id, CompleteInspectionRequest request, CancellationToken cancellationToken)
    {
        var inspection = await Query().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (inspection is null)
        {
            return ApplicationInspectionResult.Failure("Inspection was not found.", notFound: true);
        }

        inspection.Complete(request.Responses.Select(item =>
            new EthioFleet.Domain.Inspections.InspectionItemResponse(item.Key, item.Result, item.Notes)).ToArray(), clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ApplicationInspectionResult.Success(ToResponse(inspection));
    }

    private IQueryable<VehicleInspection> Query() => dbContext.VehicleInspections
        .Include(item => item.Vehicle);

    private static InspectionResponse ToResponse(VehicleInspection inspection) => new(
        inspection.Id,
        inspection.OrganizationId,
        inspection.VehicleId,
        inspection.Vehicle.PlateNumber,
        inspection.InspectorUserId,
        inspection.InspectedAt,
        inspection.Odometer,
        inspection.ChecklistVersion,
        inspection.ChecklistItems,
        inspection.Responses,
        inspection.Status,
        inspection.RequiresVehicleOutOfService,
        inspection.CompletedAt,
        string.IsNullOrEmpty(inspection.Notes) ? null : inspection.Notes);
}
