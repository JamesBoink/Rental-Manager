using RentalManager.Application.Contracts;

namespace RentalManager.Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(
        int page, int pageSize, bool? activeOnly = null, string? search = null,
        CancellationToken cancellationToken = default);

    Task<CustomerDto?> GetCustomerAsync(int id, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDto?> UpdateCustomerAsync(
        int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<bool?> DeactivateCustomerAsync(int id, CancellationToken cancellationToken = default);
}
