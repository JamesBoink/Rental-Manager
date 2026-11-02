using Microsoft.EntityFrameworkCore;
using RentalManager.Domain.Entities;

namespace RentalManager.Infrastructure.Persistence;

public class RentalDbContext(DbContextOptions<RentalDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<EquipmentCategory> EquipmentCategories => Set<EquipmentCategory>();

    public DbSet<EquipmentItem> EquipmentItems => Set<EquipmentItem>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<ReservationLine> ReservationLines => Set<ReservationLine>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RentalDbContext).Assembly);
    }
}
