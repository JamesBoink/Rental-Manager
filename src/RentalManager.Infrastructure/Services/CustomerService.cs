using Microsoft.EntityFrameworkCore;
using RentalManager.Application.Common;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;
using RentalManager.Domain.Entities;
using RentalManager.Infrastructure.Persistence;

namespace RentalManager.Infrastructure.Services;

public sealed class CustomerService(RentalDbContext db) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(
        int page,
        int pageSize,
        bool? activeOnly = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var query = db.Customers.AsQueryable();

        if (activeOnly.HasValue)
        {
            query = query.Where(x => x.IsActive == activeOnly.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Email.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CustomerDto(
                x.Id,
                x.Name,
                x.Email,
                x.Phone,
                x.TaxId,
                x.Address,
                x.IsActive,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items, page, pageSize, total);
    }

    public async Task<CustomerDto?> GetCustomerAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var customer = await db.Customers.FindAsync([id], cancellationToken);

        return customer is null ? null : Map(customer);
    }

    public async Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Customers.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        var customer = new Customer(request.Name, email, DateTime.UtcNow);
        customer.SetContactDetails(request.Phone, request.TaxId, request.Address);

        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);

        return Map(customer);
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(
        int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await db.Customers.FindAsync([id], cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var email = NormalizeEmail(request.Email);

        if (await db.Customers.AnyAsync(x => x.Email == email && x.Id != id, cancellationToken))
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        customer.UpdateDetails(request.Name, email, request.Phone, request.TaxId, request.Address);
        await db.SaveChangesAsync(cancellationToken);

        return Map(customer);
    }

    public async Task<bool?> DeactivateCustomerAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var customer = await db.Customers.FindAsync([id], cancellationToken);
        if (customer is null)
        {
            return null;
        }

        if (!customer.IsActive)
        {
            return false;
        }

        customer.Deactivate();
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static CustomerDto Map(Customer customer) =>
        new(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.TaxId,
            customer.Address,
            customer.IsActive,
            customer.CreatedAt);
}
