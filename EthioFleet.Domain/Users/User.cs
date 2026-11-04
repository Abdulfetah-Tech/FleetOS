using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Users;

public sealed class User : BaseEntity, ITenantOwned, ISoftDeletable
{
    private User()
    {
        Email = string.Empty;
        UserName = string.Empty;
        NormalizedEmail = string.Empty;
        NormalizedUserName = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string email, string userName, Guid organizationId)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("User name is required.");
        }

        Email = email.Trim().ToLowerInvariant();
        UserName = userName.Trim();
        NormalizedEmail = Email.ToUpperInvariant();
        NormalizedUserName = UserName.ToUpperInvariant();
        OrganizationId = organizationId;
        IsActive = true;
        EmailConfirmed = false;
    }

    public string Email { get; private set; }
    public string NormalizedEmail { get; private set; }
    public string UserName { get; private set; }
    public string NormalizedUserName { get; private set; }
    public Guid OrganizationId { get; set; }
    public bool IsActive { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }
}
