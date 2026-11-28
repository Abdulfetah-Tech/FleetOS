using EthioFleet.Application.Maintenance;
using FluentValidation;

namespace EthioFleet.Application.Maintenance;

public sealed class CreateMaintenanceScheduleRequestValidator : AbstractValidator<CreateMaintenanceScheduleRequest>
{
    public CreateMaintenanceScheduleRequestValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("VehicleId is required.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160).WithMessage("Name is required and cannot exceed 160 characters.");
        RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x.NextDueDate).NotNull().When(x => x.NextDueMileage == null && x.IntervalDays == null && x.IntervalMileage == null)
            .WithMessage("NextDueDate is required when no mileage or interval is specified.");
        RuleFor(x => x.NextDueMileage).GreaterThanOrEqualTo(0).When(x => x.NextDueMileage.HasValue);
        RuleFor(x => x.IntervalDays).GreaterThan(0).When(x => x.IntervalDays.HasValue);
        RuleFor(x => x.IntervalMileage).GreaterThan(0).When(x => x.IntervalMileage.HasValue);
    }
}