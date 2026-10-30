using Microsoft.EntityFrameworkCore;

namespace RentalManager.Infrastructure.Persistence;

public class RentalDbContext(DbContextOptions<RentalDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RentalDbContext).Assembly);
    }
}
