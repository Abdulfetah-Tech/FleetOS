using EthioFleet.Application.Maintenance;
using FluentValidation;

namespace EthioFleet.Application.Maintenance;

public sealed class CreateMaintenanceRecordRequestValidator : AbstractValidator<CreateMaintenanceRecordRequest>
{
    public CreateMaintenanceRecordRequestValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("VehicleId is required.");
        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid maintenance type.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500).WithMessage("Description is required and cannot exceed 500 characters.");
        RuleFor(x => x.PerformedOn).NotEmpty().WithMessage("PerformedOn date is required.");
        RuleFor(x => x.Odometer).GreaterThanOrEqualTo(0).WithMessage("Odometer must be non-negative.");
        RuleFor(x => x.LaborCost).GreaterThanOrEqualTo(0).WithMessage("LaborCost must be non-negative.");
        RuleFor(x => x.PartsCost).GreaterThanOrEqualTo(0).WithMessage("PartsCost must be non-negative.");
        RuleFor(x => x.CurrencyCode).Length(3).WithMessage("CurrencyCode must be 3 letters.");
        RuleFor(x => x.ServiceProvider).MaximumLength(160).When(x => !string.IsNullOrWhiteSpace(x.ServiceProvider));
        RuleFor(x => x.Reference).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Reference));
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}