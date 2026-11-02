using RentalManager.Domain.Common;
using RentalManager.Domain.Entities;

namespace RentalManager.Tests.Domain;

public class ReservationTests
{
    private static readonly DateTime From = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = new(2026, 3, 5, 16, 0, 0, DateTimeKind.Utc);

    private static Customer CreateCustomer() =>
        new("Buildtech Sp. z o.o.", "office@buildtech.pl", DateTime.UtcNow);

    private static EquipmentItem CreateEquipment(decimal dailyRate = 100m)
    {
        var category = new EquipmentCategory("Power tools");
        return new EquipmentItem(category, "Rotary hammer", dailyRate);
    }

    [Fact]
    public void NewReservationStartsAsDraft()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);

        Assert.Equal(ReservationStatus.Draft, reservation.Status);
        Assert.Empty(reservation.Lines);
    }

    [Fact]
    public void EndDateBeforeStartDateIsRejected()
    {
        Assert.Throws<DomainException>(() => new Reservation(CreateCustomer(), "R-2026-001", To, From));
    }

    [Fact]
    public void EmptyReferenceIsRejected()
    {
        Assert.Throws<DomainException>(() => new Reservation(CreateCustomer(), "  ", From, To));
    }

    [Fact]
    public void LineMustFitInsideTheReservationPeriod()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);
        var equipment = CreateEquipment();

        Assert.Throws<DomainException>(() =>
            reservation.AddLine(equipment, From.AddDays(-1), To));

        Assert.Throws<DomainException>(() =>
            reservation.AddLine(equipment, From, To.AddDays(1)));
    }

    [Fact]
    public void QuantityBelowOneIsRejected()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);

        Assert.Throws<DomainException>(() =>
            reservation.AddLine(CreateEquipment(), From, To, 0));
    }

    [Fact]
    public void TotalAmountIsTheSumOfTheLines()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);

        reservation.AddLine(CreateEquipment(100m), From, From.AddDays(3), 2);
        reservation.AddLine(CreateEquipment(50m), From, From.AddDays(1));

        // 100 * 3 days * 2 units + 50 * 1 day
        Assert.Equal(650m, reservation.TotalAmount);
    }

    [Fact]
    public void ReservationWithoutLinesCannotBeConfirmed()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);

        Assert.Throws<DomainException>(() => reservation.Confirm());
    }

    [Fact]
    public void ConfirmRequiresDraftStatus()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);
        reservation.AddLine(CreateEquipment(), From, To);
        reservation.Confirm();

        Assert.Throws<DomainException>(() => reservation.Confirm());
    }

    [Fact]
    public void ConfirmedReservationCannotBeModified()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);
        reservation.AddLine(CreateEquipment(), From, To);
        reservation.Confirm();

        Assert.Throws<DomainException>(() =>
            reservation.AddLine(CreateEquipment(), From, To));
    }

    [Fact]
    public void StatusMovesThroughTheExpectedOrder()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);
        reservation.AddLine(CreateEquipment(), From, To);

        reservation.Confirm();
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);

        reservation.Start();
        Assert.Equal(ReservationStatus.Active, reservation.Status);

        reservation.Complete();
        Assert.Equal(ReservationStatus.Completed, reservation.Status);
    }

    [Fact]
    public void CompletedReservationCannotBeCancelled()
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, To);
        reservation.AddLine(CreateEquipment(), From, To);
        reservation.Confirm();
        reservation.Start();
        reservation.Complete();

        Assert.Throws<DomainException>(() => reservation.Cancel());
    }

    [Theory]
    [InlineData(-1, 0, false)]
    [InlineData(-1, 1, true)]
    [InlineData(0, 1, true)]
    [InlineData(2, 4, true)]
    [InlineData(3, 4, false)]
    [InlineData(4, 5, false)]
    public void OverlapsUsesHalfOpenIntervals(int otherFromOffset, int otherToOffset, bool expected)
    {
        var reservation = new Reservation(CreateCustomer(), "R-2026-001", From, From.AddDays(3));

        var overlaps = reservation.Overlaps(
            From.AddDays(otherFromOffset),
            From.AddDays(otherToOffset));

        Assert.Equal(expected, overlaps);
    }
}
