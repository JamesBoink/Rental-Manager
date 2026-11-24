using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Validation;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/equipment")]
public sealed class EquipmentController(
    ICatalogueService service,
    IValidator<CreateEquipmentRequest> createValidator,
    IValidator<UpdateEquipmentRequest> updateValidator,
    IValidator<UpdateEquipmentStatusRequest> statusValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EquipmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EquipmentDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.GetEquipmentAsync(
            page, pageSize, categoryId, status, search, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDto>> Get(
        int id, CancellationToken cancellationToken = default)
    {
        var item = await service.GetEquipmentAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDto>> Create(
        [FromBody] CreateEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        var item = await service.CreateEquipmentAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDto>> Update(
        int id,
        [FromBody] UpdateEquipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        var item = await service.UpdateEquipmentAsync(id, request, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(EquipmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDto>> ChangeStatus(
        int id,
        [FromBody] UpdateEquipmentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        var item = await service.ChangeEquipmentStatusAsync(id, request.Status, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }
}
