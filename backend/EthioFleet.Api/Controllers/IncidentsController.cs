using Asp.Versioning;
using EthioFleet.Application.Incidents;
using EthioFleet.Domain.Incidents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/incidents")]
[Authorize(Policy = "FleetManager")]
public sealed class IncidentsController(IIncidentService incidentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IncidentPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IncidentPage>> List(
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] IncidentStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await incidentService.ListAsync(vehicleId, status, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await incidentService.GetAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Incident) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> Create(CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        var result = await incidentService.CreateAsync(request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }
        return CreatedAtAction(nameof(Get), new { id = result.Incident!.Id, version = "1.0" }, result.Incident);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> UpdateStatus(
        Guid id,
        UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await incidentService.UpdateStatusAsync(id, request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }
        return result.Succeeded ? Ok(result.Incident) : BadRequest(new { error = result.Error });
    }
}
