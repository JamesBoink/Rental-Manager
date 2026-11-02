using RentalManager.Domain.Common;

namespace RentalManager.Domain.Entities;

public class ReservationLine
{
    private ReservationLine()
    {
    }

    internal ReservationLine(Reservation reservation, EquipmentItem equipment, DateTime from, DateTime to, int quantity)
    {
        if (to <= from)
        {
            throw new DomainException("Rental end date must be after the start date.");
        }

        Reservation = reservation;
        Equipment = equipment;
        From = from;
        To = to;
        Quantity = quantity;
        UnitPrice = equipment.DailyRate;
    }

    public int Id { get; private set; }

    public int ReservationId { get; private set; }

    public Reservation Reservation { get; private set; } = null!;

    public int EquipmentId { get; private set; }

    public EquipmentItem Equipment { get; private set; } = null!;

    public DateTime From { get; private set; }

    public DateTime To { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int Days => (int)(To - From).TotalDays;

    public decimal TotalAmount => UnitPrice * Days * Quantity;
}
