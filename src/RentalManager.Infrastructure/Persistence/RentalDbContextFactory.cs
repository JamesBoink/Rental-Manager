using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RentalManager.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without booting the API host.
/// </summary>
public class RentalDbContextFactory : IDesignTimeDbContextFactory<RentalDbContext>
{
    public RentalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RentalDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3306;Database=rental_manager;User Id=root;Password=root",
            new MySqlServerVersion(new Version(8, 0, 36)));

        return new RentalDbContext(optionsBuilder.Options);
    }
}
