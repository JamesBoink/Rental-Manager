using RentalManager.Domain.Common;

namespace RentalManager.Domain.Entities;

public enum EquipmentStatus
{
    Available = 0,
    Rented = 1,
    Maintenance = 2,
    Retired = 3
}

public class EquipmentItem
{
    private EquipmentItem()
    {
    }

    public EquipmentItem(EquipmentCategory category, string name, decimal dailyRate, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Equipment name is required.");
        }

        if (dailyRate < 0)
        {
            throw new DomainException("Daily rate cannot be negative.");
        }

        Category = category;
        Name = name.Trim();
        DailyRate = dailyRate;
        Description = description?.Trim();
        Status = EquipmentStatus.Available;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int CategoryId { get; private set; }

    public EquipmentCategory Category { get; private set; } = null!;

    public decimal DailyRate { get; private set; }

    public EquipmentStatus Status { get; private set; }

    public ICollection<ReservationLine> ReservationLines { get; private set; } = new List<ReservationLine>();

    public void ChangeStatus(EquipmentStatus status)
    {
        if (Status == EquipmentStatus.Rented && status == EquipmentStatus.Retired)
        {
            throw new DomainException("Equipment that is currently rented cannot be retired.");
        }

        Status = status;
    }

    public void ChangeDailyRate(decimal dailyRate)
    {
        if (dailyRate < 0)
        {
            throw new DomainException("Daily rate cannot be negative.");
        }

        DailyRate = dailyRate;
    }
}
