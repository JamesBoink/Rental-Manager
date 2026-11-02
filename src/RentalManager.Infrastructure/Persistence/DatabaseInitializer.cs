using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RentalManager.Infrastructure.Persistence;

/// <summary>
/// Brings the database up to date and inserts reference data.
/// </summary>
public class DatabaseInitializer(
    RentalDbContext db,
    ILogger<DatabaseInitializer> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        SeedData.Apply(db);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Database schema is up to date and reference data is in place.");
    }
}
