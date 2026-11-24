using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RentalManager.Application.Interfaces;
using RentalManager.Domain.Entities;

namespace RentalManager.Infrastructure.Persistence;

/// <summary>
/// Brings the database up to date and inserts reference data.
/// </summary>
public class DatabaseInitializer(
    RentalDbContext db,
    IPasswordHasher hasher,
    IConfiguration configuration,
    ILogger<DatabaseInitializer> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        SeedData.Apply(db);
        SeedAdminUser();

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Database schema is up to date and reference data is in place.");
    }

    private void SeedAdminUser()
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (db.Users.Any(x => x.Email == email.Trim().ToLowerInvariant()))
        {
            return;
        }

        db.Users.Add(new User(
            email,
            hasher.Hash(password),
            "Administrator",
            UserRole.Admin));
    }
}
