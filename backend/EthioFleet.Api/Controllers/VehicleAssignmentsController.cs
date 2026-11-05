using Asp.Versioning;
using EthioFleet.Application.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vehicles/{vehicleId:guid}/assignment")]
[Authorize(Policy = "FleetManager")]
public sealed class VehicleAssignmentsController(IVehicleAssignmentService assignmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<VehicleAssignmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleAssignmentResponse>> Get(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await assignmentService.GetActiveForVehicleAsync(vehicleId, cancellationToken);
        return result.Succeeded ? Ok(result.Assignment) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<VehicleAssignmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleAssignmentResponse>> Assign(
        Guid vehicleId,
        AssignVehicleRequest request,
        CancellationToken cancellationToken)
    {
        if (vehicleId != request.VehicleId)
        {
            return BadRequest(new { error = "Route vehicle ID does not match the request vehicle ID." });
        }

        try
        {
            var result = await assignmentService.AssignAsync(
                new CreateVehicleAssignmentRequest(request.VehicleId, request.DriverId, request.Notes),
                cancellationToken);
            if (result.NotFound)
            {
                return NotFound(new { error = result.Error });
            }

            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.Error });
            }

            return CreatedAtAction(nameof(Get), new { vehicleId, version = "1.0" }, result.Assignment);
        }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation
        })
        {
            return Conflict(new { error = "Vehicle or driver already has an active assignment, or the tenant references are inconsistent." });
        }
    }

    [HttpDelete]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<VehicleAssignmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleAssignmentResponse>> Unassign(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await assignmentService.UnassignAsync(vehicleId, cancellationToken);
        return result.Succeeded
            ? Ok(result.Assignment)
            : NotFound(new { error = result.Error });
    }
}

public sealed record AssignVehicleRequest(Guid VehicleId, Guid DriverId, string? Notes = null);
