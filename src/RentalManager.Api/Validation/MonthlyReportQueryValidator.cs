using FluentValidation;
using RentalManager.Application.Contracts;

namespace RentalManager.Api.Validation;

public sealed class MonthlyReportQueryValidator : AbstractValidator<MonthlyReportQuery>
{
    public MonthlyReportQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
