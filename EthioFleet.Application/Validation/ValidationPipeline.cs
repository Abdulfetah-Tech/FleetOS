using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace EthioFleet.Application.Validation;

public sealed class ValidationPipeline(IServiceProvider serviceProvider) : IValidationPipeline
{
    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var failures = new List<ValidationFailure>();
        foreach (var validator in serviceProvider.GetServices<IValidator<T>>())
        {
            var result = await validator.ValidateAsync(instance, cancellationToken);
            failures.AddRange(result.Errors);
        }

        return new ValidationResult(failures);
    }
}
