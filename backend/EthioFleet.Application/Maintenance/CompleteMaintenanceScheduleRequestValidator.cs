using EthioFleet.Application.Maintenance;
using FluentValidation;

namespace EthioFleet.Application.Maintenance;

public sealed class CompleteMaintenanceScheduleRequestValidator : AbstractValidator<CompleteMaintenanceScheduleRequest>
{
    public CompleteMaintenanceScheduleRequestValidator()
    {
        RuleFor(x => x.CompletedOn).NotEmpty().WithMessage("CompletedOn date is required.");
        RuleFor(x => x.CompletedAtMileage).GreaterThanOrEqualTo(0).WithMessage("CompletedAtMileage must be non-negative.");
    }
}