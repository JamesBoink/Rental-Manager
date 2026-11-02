using Microsoft.EntityFrameworkCore;
using RentalManager.Domain.Entities;
using RentalManager.Infrastructure.Persistence;

namespace RentalManager.Tests.Infrastructure;

public class RentalDbContextTests : IDisposable
{
    private readonly RentalDbContext _db;

    public RentalDbContextTests()
    {
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseMySql(
                "Server=localhost;Port=3306;Database=rental_manager_test;User Id=root;Password=root",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        _db = new RentalDbContext(options);
        _db.Database.EnsureDeleted();
        _db.Database.Migrate();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CustomerAndEquipmentRoundTripThroughTheDatabase()
    {
        var category = new EquipmentCategory("Scaffolding", "Towers and props");
        _db.EquipmentCategories.Add(category);
        await _db.SaveChangesAsync();

        var equipment = new EquipmentItem(category, "Tower scaffold 2x2m", 120m, "Aluminium");
        _db.EquipmentItems.Add(equipment);
        await _db.SaveChangesAsync();

        var customer = new Customer("Buildtech Sp. z o.o.", "office@buildtech.pl", DateTime.UtcNow);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        var loaded = await _db.EquipmentItems
            .Include(x => x.Category)
            .SingleAsync();

        Assert.Equal("Tower scaffold 2x2m", loaded.Name);
        Assert.Equal(120m, loaded.DailyRate);
        Assert.Equal("Scaffolding", loaded.Category.Name);
        Assert.Equal(EquipmentStatus.Available, loaded.Status);
        Assert.True(await _db.Customers.AnyAsync(x => x.Email == "office@buildtech.pl"));
    }

    [Fact]
    public async Task ReservationKeepsItsLinesAndTotal()
    {
        var category = new EquipmentCategory("Power tools");
        _db.EquipmentCategories.Add(category);
        await _db.SaveChangesAsync();

        var drill = new EquipmentItem(category, "Rotary hammer", 95m);
        _db.EquipmentItems.Add(drill);
        await _db.SaveChangesAsync();

        var customer = new Customer("Marta Kowalczyk", "m.kowalczyk@example.pl", DateTime.UtcNow);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        var from = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        var reservation = new Reservation(customer, "R-2026-001", from, to);
        reservation.AddLine(drill, from, to, 2);
        reservation.Confirm();

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        var loaded = await _db.Reservations
            .Include(x => x.Lines)
            .SingleAsync();

        Assert.Equal(ReservationStatus.Confirmed, loaded.Status);
        Assert.Single(loaded.Lines);
        Assert.Equal(2, loaded.Lines.First().Quantity);
        Assert.Equal(3, loaded.Lines.First().Days);
        Assert.Equal(570m, loaded.TotalAmount);
    }

    [Fact]
    public async Task EquipmentStatusIsStoredAsText()
    {
        var category = new EquipmentCategory("Site equipment");
        _db.EquipmentCategories.Add(category);
        await _db.SaveChangesAsync();

        var generator = new EquipmentItem(category, "Petrol generator 5kVA", 210m);
        generator.ChangeStatus(EquipmentStatus.Maintenance);
        _db.EquipmentItems.Add(generator);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var loaded = await _db.EquipmentItems.SingleAsync();

        Assert.Equal(EquipmentStatus.Maintenance, loaded.Status);
    }

    [Fact]
    public async Task CustomerEmailIsUnique()
    {
        _db.Customers.Add(new Customer("Buildtech", "office@buildtech.pl", DateTime.UtcNow));
        await _db.SaveChangesAsync();

        _db.Customers.Add(new Customer("Buildtech Two", "office@buildtech.pl", DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }
}
