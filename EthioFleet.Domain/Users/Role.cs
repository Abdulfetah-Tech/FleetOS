using EthioFleet.Domain.Common;

namespace EthioFleet.Domain.Users;

public sealed class Role : BaseEntity
{
    private Role()
    {
        Name = string.Empty;
        NormalizedName = string.Empty;
        Description = string.Empty;
    }

    public Role(string name, string? description = null, bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name is required.");
        }

        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        Description = description ?? string.Empty;
        IsSystemRole = isSystemRole;
    }

    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public string Description { get; private set; }
    public bool IsSystemRole { get; private set; }

    public void Update(string? name = null, string? description = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
            NormalizedName = Name.ToUpperInvariant();
        }

        if (description is not null)
        {
            Description = description.Trim();
        }
    }
}
