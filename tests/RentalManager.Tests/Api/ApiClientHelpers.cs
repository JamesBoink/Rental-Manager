using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Application.Contracts;
using RentalManager.Application.Interfaces;
using RentalManager.Domain.Entities;

namespace RentalManager.Tests.Api;

internal static class ApiClientHelpers
{
    private const string AdminPassword = "integration-test-password";

    public static async Task<HttpClient> AdminClientAsync(ApiFactory factory)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("admin@rentalmanager.local", AdminPassword));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (body is null)
        {
            throw new InvalidOperationException("Admin login failed.");
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.AccessToken);

        return client;
    }

    public static HttpClient StaffClient(ApiFactory factory)
    {
        string token;

        using (var scope = factory.Services.CreateScope())
        {
            var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
            var staff = new User("staff@rentalmanager.local", "hash", "Staff user", UserRole.Staff);
            token = generator.Generate(staff).Token;
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
