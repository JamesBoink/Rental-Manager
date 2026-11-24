using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Infrastructure.Persistence;

namespace RentalManager.Tests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"rental-api-{Guid.NewGuid():N}";

    public ApiFactory()
    {
        var settings = new Dictionary<string, string>
        {
            // AddInfrastructure requires a connection string before the test
            // factory replaces the provider; this value is never connected to.
            ["ConnectionStrings__MySql"] = "Server=localhost;Database=unused;User Id=unused;Password=unused",
            ["Jwt__Key"] = "integration-test-key-long-enough-for-hs256-algorithms",
            ["Jwt__Issuer"] = "RentalManager",
            ["Jwt__Audience"] = "RentalManagerClients",
            ["Swagger__Enabled"] = "false",
            ["Seed__AdminEmail"] = "admin@rentalmanager.local",
            ["Seed__AdminPassword"] = "integration-test-password"
        };

        foreach (var (key, value) in settings)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                x => x.ServiceType == typeof(DbContextOptions<RentalDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RentalDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
