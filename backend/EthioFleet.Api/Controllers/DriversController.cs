using Asp.Versioning;
using EthioFleet.Application.Drivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/drivers")]
[Authorize(Policy = "FleetManager")]
public sealed class DriversController(IDriverService driverService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DriverPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverPage>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await driverService.ListAsync(new DriverListQuery(page, pageSize, search), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DriverResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await driverService.GetAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Driver) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<DriverResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DriverResponse>> Create(CreateDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await driverService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Driver!.Id, version = "1.0" }, result.Driver);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<DriverResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverResponse>> Update(Guid id, UpdateDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await driverService.UpdateAsync(id, request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Driver);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<DriverResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverResponse>> ChangeStatus(Guid id, DriverStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await driverService.ChangeStatusAsync(id, request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Driver);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await driverService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
