using System.Reflection;
using EthioFleet.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EthioFleet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IValidationPipeline, ValidationPipeline>();
        return services;
    }
}
