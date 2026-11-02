using RentalManager.Domain.Common;
using RentalManager.Domain.Entities;

namespace RentalManager.Tests.Domain;

public class CustomerTests
{
    [Fact]
    public void NewCustomerIsActive()
    {
        var customer = new Customer("Buildtech Sp. z o.o.", "Office@Buildtech.pl", DateTime.UtcNow);

        Assert.True(customer.IsActive);
        Assert.Equal("office@buildtech.pl", customer.Email);
    }

    [Fact]
    public void EmptyNameIsRejected()
    {
        Assert.Throws<DomainException>(() => new Customer(" ", "office@buildtech.pl", DateTime.UtcNow));
    }

    [Fact]
    public void EmptyEmailIsRejected()
    {
        Assert.Throws<DomainException>(() => new Customer("Buildtech", "  ", DateTime.UtcNow));
    }

    [Fact]
    public void DeactivateClearsTheActiveFlag()
    {
        var customer = new Customer("Buildtech", "office@buildtech.pl", DateTime.UtcNow);

        customer.Deactivate();

        Assert.False(customer.IsActive);
    }
}
