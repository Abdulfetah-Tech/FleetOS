using System.Security.Claims;
using EthioFleet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/current")]
[Authorize(Policy = "TenantMember")]
public sealed class CurrentOrganizationController(ITenantContext tenantContext, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public IActionResult GetCurrent()
    {
        if (tenantContext.OrganizationId is not Guid organizationId || currentUser.UserId is not Guid userId)
        {
            return Forbid();
        }

        return Ok(new CurrentOrganizationResponse(organizationId, userId, currentUser.Email));
    }

    [HttpGet("fleet")]
    [Authorize(Policy = "FleetManager")]
    public IActionResult GetFleetWorkspace() => Ok(new { organizationId = tenantContext.OrganizationId });
}

public sealed record CurrentOrganizationResponse(Guid OrganizationId, Guid UserId, string? Email);
