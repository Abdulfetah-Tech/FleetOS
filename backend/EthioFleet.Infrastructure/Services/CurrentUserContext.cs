using System.Security.Claims;
using EthioFleet.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EthioFleet.Infrastructure.Services;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUser, ITenantContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
        ? userId
        : null;

    public Guid? OrganizationId => Guid.TryParse(User?.FindFirst("organization_id")?.Value, out var organizationId)
        ? organizationId
        : null;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsSystemAdmin => bool.TryParse(User?.FindFirst("is_system_admin")?.Value, out var isSystemAdmin)
        && isSystemAdmin;
}
