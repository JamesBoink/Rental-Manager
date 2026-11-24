using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Validation;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
public sealed class ReservationsController(
    IReservationService service,
    IValidator<CreateReservationRequest> createValidator,
    IValidator<CreateReservationLineRequest> lineValidator,
    IValidator<AvailabilityQuery> availabilityValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReservationDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.GetReservationsAsync(
            page, pageSize, status, customerId, from, to, cancellationToken));
    }

    [HttpGet("availability")]
    [ProducesResponseType(typeof(AvailabilityResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailabilityResultDto>> Availability(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        var validation = await availabilityValidator.ValidateAsync(
            new AvailabilityQuery(from, to), cancellationToken);

        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        return Ok(await service.CheckAvailabilityAsync(from, to, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> Get(
        int id, CancellationToken cancellationToken = default)
    {
        var reservation = await service.GetReservationAsync(id, cancellationToken);

        return reservation is null ? NotFound() : Ok(reservation);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> Create(
        [FromBody] CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        var reservation = await service.CreateReservationAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = reservation.Id }, reservation);
    }

    [HttpPost("{id:int}/lines")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> AddLine(
        int id,
        [FromBody] CreateReservationLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await lineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        var reservation = await service.AddLineAsync(id, request, cancellationToken);

        return reservation is null ? NotFound() : Ok(reservation);
    }

    [HttpPost("{id:int}/confirm")]
    public Task<ActionResult<ReservationDto>> Confirm(
        int id, CancellationToken cancellationToken = default) =>
        ChangeStatus(id, service.ConfirmAsync, cancellationToken);

    [HttpPost("{id:int}/start")]
    public Task<ActionResult<ReservationDto>> Start(
        int id, CancellationToken cancellationToken = default) =>
        ChangeStatus(id, service.StartAsync, cancellationToken);

    [HttpPost("{id:int}/complete")]
    public Task<ActionResult<ReservationDto>> Complete(
        int id, CancellationToken cancellationToken = default) =>
        ChangeStatus(id, service.CompleteAsync, cancellationToken);

    [HttpPost("{id:int}/cancel")]
    public Task<ActionResult<ReservationDto>> Cancel(
        int id, CancellationToken cancellationToken = default) =>
        ChangeStatus(id, service.CancelAsync, cancellationToken);

    private async Task<ActionResult<ReservationDto>> ChangeStatus(
        int id,
        Func<int, CancellationToken, Task<ReservationDto?>> transition,
        CancellationToken cancellationToken)
    {
        var reservation = await transition(id, cancellationToken);

        return reservation is null ? NotFound() : Ok(reservation);
    }
}
