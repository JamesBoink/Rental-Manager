using System.Net;
using System.Net.Http.Headers;
using RentalManager.Application.Interfaces;
using RentalManager.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace RentalManager.Tests.Api;

public class RolePolicyTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RolePolicyTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StaffTokenIsForbiddenFromManagerOrAdminEndpoint()
    {
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
            var staff = new User("staff@rentalmanager.local", "hash", "Staff user", UserRole.Staff);

            token = generator.Generate(staff).Token;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/system/diagnostics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
