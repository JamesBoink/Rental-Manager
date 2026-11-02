using RentalManager.Domain.Common;

namespace RentalManager.Domain.Entities;

public class Customer
{
    private Customer()
    {
    }

    public Customer(string name, string email, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Customer email is required.");
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = null;
        TaxId = null;
        Address = null;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? TaxId { get; private set; }

    public string? Address { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAt { get; private set; }

    public ICollection<Reservation> Reservations { get; private set; } = new List<Reservation>();

    public void UpdateDetails(string name, string email, string? phone, string? taxId, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Customer email is required.");
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone;
        TaxId = taxId;
        Address = address;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
