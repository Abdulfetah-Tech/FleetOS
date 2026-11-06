using Asp.Versioning;
using EthioFleet.Application.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/maintenance")]
[Authorize(Policy = "MaintenanceManager")]
public sealed class MaintenanceController(IMaintenanceService maintenanceService) : ControllerBase
{
    [HttpGet("records")]
    [ProducesResponseType<MaintenanceRecordPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceRecordPage>> ListRecords(
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await maintenanceService.ListRecordsAsync(vehicleId, page, pageSize, cancellationToken));

    [HttpPost("records")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<MaintenanceRecordResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceRecordResponse>> CreateRecord(
        CreateMaintenanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await maintenanceService.CreateRecordAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ListRecords), new { vehicleId = record.VehicleId, version = "1.0" }, record);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpGet("schedules")]
    [ProducesResponseType<MaintenanceSchedulePage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenanceSchedulePage>> ListSchedules(
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] bool dueOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await maintenanceService.ListSchedulesAsync(vehicleId, dueOnly, page, pageSize, cancellationToken));

    [HttpPost("schedules")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<MaintenanceScheduleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceScheduleResponse>> CreateSchedule(
        CreateMaintenanceScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var schedule = await maintenanceService.CreateScheduleAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ListSchedules), new { vehicleId = schedule.VehicleId, version = "1.0" }, schedule);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpPost("schedules/{id:guid}/complete")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<MaintenanceScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceScheduleResponse>> CompleteSchedule(
        Guid id,
        CompleteMaintenanceScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await maintenanceService.CompleteScheduleAsync(id, request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Item);
    }
}
