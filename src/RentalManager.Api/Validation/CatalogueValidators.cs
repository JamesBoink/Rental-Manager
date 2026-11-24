using FluentValidation;
using RentalManager.Application.Contracts;
using RentalManager.Domain.Entities;

namespace RentalManager.Api.Validation;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

public class CreateEquipmentRequestValidator : AbstractValidator<CreateEquipmentRequest>
{
    public CreateEquipmentRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DailyRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

public class UpdateEquipmentRequestValidator : AbstractValidator<UpdateEquipmentRequest>
{
    public UpdateEquipmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DailyRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

public class UpdateEquipmentStatusRequestValidator : AbstractValidator<UpdateEquipmentStatusRequest>
{
    public UpdateEquipmentStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .IsEnumName(typeof(EquipmentStatus), caseSensitive: false);
    }
}
