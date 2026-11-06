using Asp.Versioning;
using EthioFleet.Application.Fuel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fuel-records")]
[Authorize(Policy = "FleetManager")]
public sealed class FuelRecordsController(IFuelRecordService fuelRecordService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<FuelRecordPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FuelRecordPage>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] Guid? driverId = null,
        CancellationToken cancellationToken = default)
    {
        var pageResult = await fuelRecordService.ListAsync(
            new FuelRecordListQuery(page, pageSize, vehicleId, driverId), cancellationToken);
        return Ok(pageResult);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<FuelRecordResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FuelRecordResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await fuelRecordService.GetAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.FuelRecord) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<FuelRecordResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FuelRecordResponse>> Create(
        CreateFuelRecordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await fuelRecordService.CreateAsync(request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(Get), new { id = result.FuelRecord!.Id, version = "1.0" }, result.FuelRecord);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await fuelRecordService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
