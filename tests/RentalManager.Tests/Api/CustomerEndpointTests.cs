using System.Net;
using System.Net.Http.Json;
using RentalManager.Application.Contracts;

namespace RentalManager.Tests.Api;

public class CustomerEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CustomerEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousCallersCannotReadCustomers()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/customers")).StatusCode);
    }

    [Fact]
    public async Task AdminCanListAndSearchCustomers()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var all = await client.GetFromJsonAsync<PagedResult<CustomerDto>>(
            "/api/customers?page=1&pageSize=1");
        Assert.NotNull(all);
        Assert.True(all.TotalCount >= 2);
        Assert.Single(all.Items);

        var searched = await client.GetFromJsonAsync<PagedResult<CustomerDto>>(
            "/api/customers?search=kowalczyk");
        Assert.NotNull(searched);
        Assert.Single(searched.Items);
        Assert.Equal("Marta Kowalczyk", searched.Items[0].Name);
    }

    [Fact]
    public async Task AdminCanCreateCustomerButNotDuplicateEmail()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var created = await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Test Construction", "test@construction.pl", "555-100", null, null));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(body);
        Assert.Equal("test@construction.pl", body.Email);
        Assert.True(body.IsActive);

        var duplicate = await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Other Company", "  TEST@CONSTRUCTION.PL  ", null, null, null));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task AdminCanUpdateAndDeactivateCustomer()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var updated = await client.PutAsJsonAsync(
            "/api/customers/1",
            new UpdateCustomerRequest("Buildtech Updated", "buildtech@example.pl", null, "PL123", "Warsaw"));

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var body = await updated.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal("PL123", body!.TaxId);

        var deactivated = await client.PostAsync("/api/customers/1/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);

        var activeOnly = await client.GetFromJsonAsync<PagedResult<CustomerDto>>(
            "/api/customers?activeOnly=true");
        Assert.NotNull(activeOnly);
        Assert.DoesNotContain(activeOnly.Items, x => x.Id == 1);
    }

    [Fact]
    public async Task StaffCannotWriteCustomers()
    {
        var client = ApiClientHelpers.StaffClient(_factory);

        var response = await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("No access", "no@access.pl", null, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequestsReturnBadRequestOrNotFound()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var invalidEmail = await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Bad mail", "not-an-email", null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidEmail.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/customers/9999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsync("/api/customers/9999/deactivate", null)).StatusCode);
    }
}
