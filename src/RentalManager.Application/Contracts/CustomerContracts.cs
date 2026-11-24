namespace RentalManager.Application.Contracts;

public sealed record CustomerDto(
    int Id,
    string Name,
    string Email,
    string? Phone,
    string? TaxId,
    string? Address,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? TaxId,
    string? Address);

public sealed record UpdateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? TaxId,
    string? Address);
