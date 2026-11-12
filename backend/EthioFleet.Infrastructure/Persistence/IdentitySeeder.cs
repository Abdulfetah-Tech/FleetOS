using Microsoft.AspNetCore.Identity;

namespace EthioFleet.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roles =
        [
            "SuperAdministrator", "SystemAdmin", "CompanyAdministrator", "CompanyOwner",
            "FleetManager", "OperationsManager", "Dispatcher", "Driver", "Accountant",
            "MaintenanceManager", "ReadOnlyAnalyst", "Employee"
        ];
        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Could not create required role '{roleName}'.");
            }
        }
    }

    public static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not create required role '{roleName}'.");
        }
    }
}
