using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Authentication;

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppIdentityUser User { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public bool IsRevoked => RevokedAt.HasValue;
}

public sealed class AppIdentityUser : IdentityUser<Guid>
{
    public Guid OrganizationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
