using RentalManager.Domain.Common;

namespace RentalManager.Domain.Entities;

public class EquipmentCategory
{
    private EquipmentCategory()
    {
    }

    public EquipmentCategory(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.");
        }

        Name = name.Trim();
        Description = description?.Trim();
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public ICollection<EquipmentItem> Equipment { get; private set; } = new List<EquipmentItem>();
}
