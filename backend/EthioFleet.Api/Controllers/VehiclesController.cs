using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using EthioFleet.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vehicles")]
[Authorize(Policy = "FleetManager")]
public sealed class VehiclesController(IVehicleService vehicleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<VehiclePage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VehiclePage>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery, StringLength(100)] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await vehicleService.ListAsync(new VehicleListQuery(page, pageSize, search), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await vehicleService.GetAsync(id, cancellationToken);
        return result.Succeeded
            ? Ok(result.Vehicle)
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Vehicle not found", detail: result.Error);
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleResponse>> Create(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await vehicleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Vehicle!.Id, version = "1.0" }, result.Vehicle);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleResponse>> Update(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await vehicleService.UpdateAsync(id, request, cancellationToken);
        if (result.NotFound)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Vehicle not found", detail: result.Error);
        }

        return result.Succeeded
            ? Ok(result.Vehicle)
            : Problem(statusCode: StatusCodes.Status409Conflict, title: "Vehicle conflict", detail: result.Error);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VehicleResponse>> ChangeStatus(Guid id, VehicleStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await vehicleService.ChangeStatusAsync(id, request, cancellationToken);
        if (result.NotFound)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Vehicle not found", detail: result.Error);
        }

        return Ok(result.Vehicle);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await vehicleService.DeleteAsync(id, cancellationToken);
        return deleted
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Vehicle not found", detail: "Vehicle was not found.");
    }
}
