using Asp.Versioning;
using EthioFleet.Application.Trips;
using EthioFleet.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/trips")]
[Authorize(Policy = "FleetManager")]
public sealed class TripsController(ITripService tripService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TripPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TripPage>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] Guid? driverId = null,
        [FromQuery] TripStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await tripService.ListAsync(new TripListQuery(page, pageSize, vehicleId, driverId, status), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TripResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await tripService.GetAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Trip) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<TripResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripResponse>> Create(CreateTripRequest request, CancellationToken cancellationToken)
    {
        var result = await tripService.CreateAsync(request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(Get), new { id = result.Trip!.Id, version = "1.0" }, result.Trip);
    }

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType<TripResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripResponse>> Start(Guid id, CancellationToken cancellationToken)
    {
        var result = await tripService.StartAsync(id, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }
        return result.Succeeded ? Ok(result.Trip) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType<TripResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripResponse>> Complete(Guid id, CompleteTripRequest request, CancellationToken cancellationToken)
    {
        var result = await tripService.CompleteAsync(id, request, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }
        return result.Succeeded ? Ok(result.Trip) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType<TripResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await tripService.CancelAsync(id, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }
        return result.Succeeded ? Ok(result.Trip) : BadRequest(new { error = result.Error });
    }
}
