using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManager.Api.Validation;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;

namespace RentalManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController(
    ICustomerService service,
    IValidator<CreateCustomerRequest> createValidator,
    IValidator<UpdateCustomerRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.GetCustomersAsync(page, pageSize, activeOnly, search, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> Get(
        int id, CancellationToken cancellationToken = default)
    {
        var customer = await service.GetCustomerAsync(id, cancellationToken);

        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        var customer = await service.CreateCustomerAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Update(
        int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ValidationProblemFactory.Create(validation));
        }

        var customer = await service.UpdateCustomerAsync(id, request, cancellationToken);

        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        int id, CancellationToken cancellationToken = default)
    {
        var result = await service.DeactivateCustomerAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new { AlreadyInactive = result == false });
    }
}
