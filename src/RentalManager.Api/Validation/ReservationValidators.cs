using FluentValidation;
using RentalManager.Application.Contracts;

namespace RentalManager.Api.Validation;

public class CreateReservationLineRequestValidator : AbstractValidator<CreateReservationLineRequest>
{
    public CreateReservationLineRequestValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.To).GreaterThan(x => x.From)
            .WithMessage("Rental end date must be after the start date.");
    }
}

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.To).GreaterThan(x => x.From)
            .WithMessage("Reservation end date must be after the start date.");
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty();

        RuleForEach(x => x.Lines).SetValidator(new CreateReservationLineRequestValidator());
        RuleForEach(x => x.Lines)
            .Must((request, line) => line.From >= request.From && line.To <= request.To)
            .WithMessage("Equipment period must fit inside the reservation period.");
    }
}

public class AvailabilityQueryValidator : AbstractValidator<AvailabilityQuery>
{
    public AvailabilityQueryValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From)
            .WithMessage("Reservation end date must be after the start date.");
    }
}
