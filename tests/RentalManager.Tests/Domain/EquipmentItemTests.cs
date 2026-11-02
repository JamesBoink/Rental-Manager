using RentalManager.Domain.Common;
using RentalManager.Domain.Entities;

namespace RentalManager.Tests.Domain;

public class EquipmentItemTests
{
    [Fact]
    public void NewEquipmentIsAvailable()
    {
        var category = new EquipmentCategory("Power tools");
        var equipment = new EquipmentItem(category, "Rotary hammer", 95m);

        Assert.Equal(EquipmentStatus.Available, equipment.Status);
        Assert.Equal(95m, equipment.DailyRate);
    }

    [Fact]
    public void NegativeDailyRateIsRejected()
    {
        var category = new EquipmentCategory("Power tools");

        Assert.Throws<DomainException>(() => new EquipmentItem(category, "Rotary hammer", -1m));
    }

    [Fact]
    public void EmptyNameIsRejected()
    {
        var category = new EquipmentCategory("Power tools");

        Assert.Throws<DomainException>(() => new EquipmentItem(category, " ", 95m));
    }

    [Fact]
    public void RentedEquipmentCannotBeRetired()
    {
        var category = new EquipmentCategory("Power tools");
        var equipment = new EquipmentItem(category, "Rotary hammer", 95m);
        equipment.ChangeStatus(EquipmentStatus.Rented);

        Assert.Throws<DomainException>(() => equipment.ChangeStatus(EquipmentStatus.Retired));
    }

    [Fact]
    public void DailyRateCanBeChangedButNotIntoTheNegative()
    {
        var category = new EquipmentCategory("Power tools");
        var equipment = new EquipmentItem(category, "Rotary hammer", 95m);

        equipment.ChangeDailyRate(110m);
        Assert.Equal(110m, equipment.DailyRate);

        Assert.Throws<DomainException>(() => equipment.ChangeDailyRate(-5m));
    }
}
