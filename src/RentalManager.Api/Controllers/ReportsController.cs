using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Validation;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(
    IReportService service,
    IValidator<MonthlyReportQuery> validator) : ControllerBase
{
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(MonthlyReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonthlyReportDto>> Monthly(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] DateTime? asOf = null,
        CancellationToken cancellationToken = default)
    {
        var query = new MonthlyReportQuery(year, month, asOf);

        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        return Ok(await service.GetMonthlyReportAsync(query, cancellationToken));
    }
}
