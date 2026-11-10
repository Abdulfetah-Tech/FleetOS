using EthioFleet.Application.Incidents;
using EthioFleet.Application.Dashboard;
using EthioFleet.Application.Assignments;
using EthioFleet.Application.Authentication;
using EthioFleet.Application.Documents;
using EthioFleet.Application.Drivers;
using EthioFleet.Application.Fuel;
using EthioFleet.Application.Inspections;
using EthioFleet.Application.Interfaces;
using EthioFleet.Application.Maintenance;
using EthioFleet.Application.Reports;
using EthioFleet.Application.Notifications;
using EthioFleet.Application.Trips;
using EthioFleet.Application.Vehicles;
using EthioFleet.Infrastructure.Authentication;
using EthioFleet.Infrastructure.Persistence;
using EthioFleet.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EthioFleet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<CurrentUserContext>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<CurrentUserContext>());
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<CurrentUserContext>());
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        services.AddIdentityCore<AppIdentityUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IVehicleAssignmentService, VehicleAssignmentService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<IInspectionService, InspectionService>();
        services.AddScoped<IVehicleDocumentService, VehicleDocumentService>();
        services.AddSingleton<IPrivateDocumentStorage, LocalPrivateDocumentStorage>();
        services.AddScoped<IFuelRecordService, FuelRecordService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFleetReminderProcessor, FleetReminderProcessor>();
        services.AddScoped<IFleetDashboardService, FleetDashboardService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IFleetReportService, FleetReportService>();
        services.AddHostedService<FleetReminderBackgroundService>();
        return services;
    }
}
