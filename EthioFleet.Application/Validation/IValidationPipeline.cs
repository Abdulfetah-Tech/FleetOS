using FluentValidation.Results;

namespace EthioFleet.Application.Validation;

public interface IValidationPipeline
{
    Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
}
