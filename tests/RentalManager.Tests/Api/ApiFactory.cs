using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RentalManager.Tests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings__MySql"] =
                "Server=localhost;Port=3306;Database=rental_manager_api_test;User Id=root;Password=root",
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
    }
}
