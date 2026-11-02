using RentalManager.Domain.Entities;

namespace RentalManager.Infrastructure.Persistence;

/// <summary>
/// Reference data used when the database is empty. Seeded by <see cref="DatabaseInitializer"/>.
/// </summary>
internal static class SeedData
{
    public static void Apply(RentalDbContext db)
    {
        var categories = SeedCategories(db);
        SeedEquipment(db, categories);
        SeedCustomers(db);
    }

    private static Dictionary<string, EquipmentCategory> SeedCategories(RentalDbContext db)
    {
        var existing = db.EquipmentCategories.ToDictionary(x => x.Name, x => x);
        var seeded = new Dictionary<string, EquipmentCategory>();

        void Ensure(string name, string description)
        {
            if (existing.TryGetValue(name, out var category))
            {
                seeded[name] = category;
                return;
            }

            category = new EquipmentCategory(name, description);
            db.EquipmentCategories.Add(category);
            existing[name] = category;
            seeded[name] = category;
        }

        Ensure("Power tools", "Handheld and bench tools, cables and accessories");
        Ensure("Scaffolding", "Tower and frame scaffolding, planks and props");
        Ensure("Site equipment", "Compactors, mixers, generators and pumps");

        return seeded;
    }

    private static void SeedEquipment(RentalDbContext db, Dictionary<string, EquipmentCategory> categories)
    {
        if (db.EquipmentItems.Any())
        {
            return;
        }

        db.EquipmentItems.AddRange(
            new EquipmentItem(categories["Power tools"], "Cordless impact wrench", 85.00m, "18V, 1/2 inch drive"),
            new EquipmentItem(categories["Power tools"], "Rotary hammer", 95.00m, "SDS-plus, 800W"),
            new EquipmentItem(categories["Power tools"], "Angle grinder 125mm", 45.00m),
            new EquipmentItem(categories["Scaffolding"], "Tower scaffold 2x2m", 120.00m, "Aluminium, 4.2m working height"),
            new EquipmentItem(categories["Scaffolding"], "Steel prop 2m", 6.50m),
            new EquipmentItem(categories["Site equipment"], "Plate compactor", 160.00m, "500kg, reversible"),
            new EquipmentItem(categories["Site equipment"], "Petrol generator 5kVA", 210.00m, "230V, 10A outlet"));
    }

    private static void SeedCustomers(RentalDbContext db)
    {
        if (db.Customers.Any())
        {
            return;
        }

        db.Customers.AddRange(
            new Customer("Buildtech Sp. z o.o.", "office@buildtech.pl", new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)),
            new Customer("Marta Kowalczyk", "m.kowalczyk@example.pl", new DateTime(2026, 2, 3, 12, 30, 0, DateTimeKind.Utc)));
    }
}
