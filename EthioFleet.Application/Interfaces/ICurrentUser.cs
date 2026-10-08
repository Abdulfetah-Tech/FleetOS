namespace EthioFleet.Application.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    string? Email { get; }
    bool IsSystemAdmin { get; }
}
