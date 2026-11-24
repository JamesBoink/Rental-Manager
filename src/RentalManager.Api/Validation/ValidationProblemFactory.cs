using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace RentalManager.Api.Validation;

public static class ValidationProblemFactory
{
    public static ValidationProblemDetails Create(ValidationResult result) =>
        new(result.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                x => x.Key,
                x => x.Select(e => e.ErrorMessage).ToArray()));
}
