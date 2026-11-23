using System.Net;
using System.Net.Http.Json;
using RentalManager.Application.Contracts;

namespace RentalManager.Tests.Api;

public class AuthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static LoginRequest Login(string password) =>
        new("admin@rentalmanager.local", password);

    [Fact]
    public async Task LoginWithValidCredentialsReturnsToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", Login("integration-test-password"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal("Admin", body.User.Role);
        Assert.Equal("admin@rentalmanager.local", body.User.Email);
        Assert.True(body.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginWithWrongPasswordReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", Login("definitely-wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithUnknownEmailReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("nobody@rentalmanager.local", "integration-test-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("not-an-email", "password")]
    [InlineData("admin@rentalmanager.local", "")]
    public async Task LoginWithInvalidInputReturnsBadRequest(string email, string password)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpointsRejectAnonymousCallers()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/system/diagnostics")).StatusCode);
    }

    [Fact]
    public async Task MeReturnsTheAuthenticatedUser()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthenticatedUser>();

        Assert.NotNull(body);
        Assert.Equal("admin@rentalmanager.local", body.Email);
        Assert.Equal("Administrator", body.DisplayName);
        Assert.Equal("Admin", body.Role);
    }

    [Fact]
    public async Task AdminCanReachManagerOrAdminEndpoint()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/system/diagnostics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthAndSystemInfoRemainPublic()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/system/info")).StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", Login("integration-test-password"));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {body.AccessToken}");
        return client;
    }
}
