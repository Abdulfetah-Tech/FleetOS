using Asp.Versioning;
using EthioFleet.Application.Audit;
using EthioFleet.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Policy = AuthorizationPolicies.CompanyOwner)]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AuditLogPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogPage>> List(
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await auditLogService.ListAsync(entityType, action, page, pageSize, cancellationToken));
}
