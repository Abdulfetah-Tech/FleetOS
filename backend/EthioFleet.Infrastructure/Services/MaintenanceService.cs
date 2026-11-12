using EthioFleet.Application.Common;
using EthioFleet.Application.Maintenance;
using EthioFleet.Domain.Maintenance;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class MaintenanceService(AppDbContext dbContext, EthioFleet.Application.Interfaces.IClock clock)
    : IMaintenanceService
{
    public async Task<MaintenanceRecordResponse> CreateRecordAsync(
        CreateMaintenanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == request.VehicleId, cancellationToken)
            ?? throw new KeyNotFoundException("Vehicle was not found.");
        var record = new MaintenanceRecord(vehicle.OrganizationId, vehicle, request.Type, request.Description,
            request.PerformedOn, request.Odometer, request.LaborCost, request.PartsCost, request.CurrencyCode,
            request.ServiceProvider, request.Reference, request.Notes);
        dbContext.MaintenanceRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToRecordResponse(record);
    }

    public async Task<MaintenanceRecordPage> ListRecordsAsync(
        Guid? vehicleId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var window = PageWindow.Create(page, pageSize);
        IQueryable<MaintenanceRecord> records = dbContext.MaintenanceRecords.AsNoTracking().Include(record => record.Vehicle);
        if (vehicleId is Guid id)
        {
            records = records.Where(record => record.VehicleId == id);
        }

        var total = await records.CountAsync(cancellationToken);
        var items = await records.OrderByDescending(record => record.PerformedOn)
            .ThenBy(record => record.Id).Skip(window.Offset).Take(window.PageSize).ToListAsync(cancellationToken);
        return new MaintenanceRecordPage(items.Select(ToRecordResponse).ToArray(),
            window.Page, window.PageSize, total);
    }

    public async Task<MaintenanceScheduleResponse> CreateScheduleAsync(
        CreateMaintenanceScheduleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == request.VehicleId, cancellationToken)
            ?? throw new KeyNotFoundException("Vehicle was not found.");
        var schedule = new MaintenanceSchedule(vehicle.OrganizationId, vehicle, request.Name, request.Description,
            request.NextDueDate, request.NextDueMileage, request.IntervalDays, request.IntervalMileage);
        dbContext.MaintenanceSchedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToScheduleResponse(schedule, vehicle, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
    }

    public async Task<MaintenanceSchedulePage> ListSchedulesAsync(
        Guid? vehicleId, bool dueOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var window = PageWindow.Create(page, pageSize);
        IQueryable<MaintenanceSchedule> schedules = dbContext.MaintenanceSchedules.AsNoTracking().Include(schedule => schedule.Vehicle);
        if (vehicleId is Guid id)
        {
            schedules = schedules.Where(schedule => schedule.VehicleId == id);
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        if (dueOnly)
        {
            schedules = schedules.Where(schedule => schedule.Status == MaintenanceScheduleStatus.Active &&
                ((schedule.NextDueDate != null && schedule.NextDueDate <= today) ||
                 (schedule.NextDueMileage != null && schedule.NextDueMileage <= schedule.Vehicle.CurrentMileage)));
        }

        var total = await schedules.CountAsync(cancellationToken);
        var items = await schedules.OrderBy(schedule => schedule.NextDueDate)
            .ThenBy(schedule => schedule.NextDueMileage)
            .ThenBy(schedule => schedule.Id)
            .Skip(window.Offset).Take(window.PageSize).ToListAsync(cancellationToken);
        return new MaintenanceSchedulePage(items.Select(schedule =>
            ToScheduleResponse(schedule, schedule.Vehicle, today)).ToArray(),
            window.Page, window.PageSize, total);
    }

    public async Task<MaintenanceResult<MaintenanceScheduleResponse>> CompleteScheduleAsync(
        Guid scheduleId, CompleteMaintenanceScheduleRequest request, CancellationToken cancellationToken)
    {
        var schedule = await dbContext.MaintenanceSchedules.Include(item => item.Vehicle)
            .SingleOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);
        if (schedule is null)
        {
            return MaintenanceResult<MaintenanceScheduleResponse>.Failure("Maintenance schedule was not found.", notFound: true);
        }

        schedule.Complete(request.CompletedOn, request.CompletedAtMileage);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MaintenanceResult<MaintenanceScheduleResponse>.Success(ToScheduleResponse(
            schedule, schedule.Vehicle, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
    }

    private static MaintenanceRecordResponse ToRecordResponse(MaintenanceRecord record) => new(
        record.Id, record.OrganizationId, record.VehicleId, record.Vehicle.PlateNumber, record.Type,
        record.Description, record.PerformedOn, record.Odometer, record.LaborCost, record.PartsCost,
        record.TotalCost, record.CurrencyCode,
        string.IsNullOrEmpty(record.ServiceProvider) ? null : record.ServiceProvider,
        string.IsNullOrEmpty(record.Reference) ? null : record.Reference,
        string.IsNullOrEmpty(record.Notes) ? null : record.Notes);

    private static MaintenanceScheduleResponse ToScheduleResponse(
        MaintenanceSchedule schedule, EthioFleet.Domain.Vehicles.Vehicle vehicle, DateOnly today) => new(
        schedule.Id, schedule.OrganizationId, schedule.VehicleId, vehicle.PlateNumber, schedule.Name,
        string.IsNullOrEmpty(schedule.Description) ? null : schedule.Description,
        schedule.NextDueDate, schedule.NextDueMileage, schedule.IntervalDays, schedule.IntervalMileage,
        schedule.Status, schedule.IsDue(today, vehicle.CurrentMileage));
}
