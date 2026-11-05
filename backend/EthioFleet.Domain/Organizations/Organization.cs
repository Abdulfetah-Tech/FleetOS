using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Organizations;

public sealed class Organization : BaseEntity, ISoftDeletable
{
    private Organization()
    {
        Name = string.Empty;
        Code = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Address = string.Empty;
    }

    public Organization(string name, string code, string email, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Organization name is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Organization code is required.");
        }

        if (code.Length > 20)
        {
            throw new DomainException("Organization code cannot exceed 20 characters.");
        }

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Email = email.Trim();
        Phone = phone.Trim();
        Address = address.Trim();
        IsActive = true;
    }

    public string Name { get; private set; }
    public string Code { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public string Address { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public void UpdateDetails(string name, string email, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Organization name is required.");
        }

        Name = name.Trim();
        Email = email.Trim();
        Phone = phone.Trim();
        Address = address.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Delete(Guid? deletedBy)
    {
        if (IsDeleted)
        {
            throw new DomainException("Organization is already deleted.");
        }

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
