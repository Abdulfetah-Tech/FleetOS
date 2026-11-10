using Asp.Versioning;
using EthioFleet.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard/fleet")]
[Authorize(Policy = "FleetManager")]
public sealed class FleetDashboardController(IFleetDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<FleetDashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FleetDashboardResponse>> Get(
        [FromQuery] DateOnly? asOfDate = null,
        [FromQuery] DateOnly? month = null,
        CancellationToken cancellationToken = default) =>
        Ok(await dashboardService.GetAsync(new FleetDashboardRequest(asOfDate, month), cancellationToken));
}
