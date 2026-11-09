using Asp.Versioning;
using EthioFleet.Application.Inspections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inspections")]
[Authorize(Policy = "InspectionSubmitter")]
public sealed class InspectionsController(IInspectionService inspectionService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "InspectionViewer")]
    [ProducesResponseType<InspectionPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InspectionPage>> List(
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await inspectionService.ListAsync(vehicleId, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "InspectionViewer")]
    [ProducesResponseType<InspectionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InspectionResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await inspectionService.GetAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Inspection) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [ProducesResponseType<InspectionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InspectionResponse>> Create(CreateInspectionRequest request, CancellationToken cancellationToken)
    {
        var result = await inspectionService.CreateAsync(request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }
        return CreatedAtAction(nameof(Get), new { id = result.Inspection!.Id, version = "1.0" }, result.Inspection);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType<InspectionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InspectionResponse>> Complete(
        Guid id,
        CompleteInspectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inspectionService.CompleteAsync(id, request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }
        return result.Succeeded ? Ok(result.Inspection) : BadRequest(new { error = result.Error });
    }
}
