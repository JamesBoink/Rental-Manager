using System.Net;
using System.Net.Http.Json;
using RentalManager.Application.Contracts;

namespace RentalManager.Tests.Api;

public class CatalogueEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CatalogueEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousCallersCannotReadCatalogue()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/equipment")).StatusCode);
    }

    [Fact]
    public async Task AdminCanListCategoriesAndEquipmentWithPaging()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var categories = await client.GetFromJsonAsync<PagedResult<CategoryDto>>(
            "/api/categories?page=1&pageSize=2");
        var equipment = await client.GetFromJsonAsync<PagedResult<EquipmentDto>>(
            "/api/equipment?page=1&pageSize=3");

        Assert.NotNull(categories);
        Assert.True(categories.TotalCount >= 3);
        Assert.Equal(2, categories.Items.Count);
        Assert.Equal(1, categories.Page);

        Assert.NotNull(equipment);
        Assert.True(equipment.TotalCount >= 7);
        Assert.Equal(3, equipment.Items.Count);
        Assert.False(string.IsNullOrWhiteSpace(equipment.Items[0].CategoryName));
    }

    [Fact]
    public async Task AdminCanCreateCategoryButNotDuplicateName()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var created = await client.PostAsJsonAsync(
            "/api/categories", new CreateCategoryRequest("Accessories", "Bits and blades"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(body);
        Assert.Equal("Accessories", body.Name);

        var duplicate = await client.PostAsJsonAsync(
            "/api/categories", new CreateCategoryRequest("  accessories  ", null));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task StaffCannotWriteToCatalogue()
    {
        var client = ApiClientHelpers.StaffClient(_factory);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync(
                "/api/categories", new CreateCategoryRequest("No access", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync(
                "/api/equipment",
                new CreateEquipmentRequest(1, "No access", 10m, null))).StatusCode);
    }

    [Fact]
    public async Task AdminCanCreateEquipmentAndChangeItsStatus()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var created = await client.PostAsJsonAsync(
            "/api/equipment",
            new CreateEquipmentRequest(1, "Test circular saw", 70m, "240V"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var equipment = await created.Content.ReadFromJsonAsync<EquipmentDto>();
        Assert.NotNull(equipment);
        Assert.Equal("Available", equipment.Status);
        Assert.False(string.IsNullOrWhiteSpace(equipment.CategoryName));

        var patched = await client.PatchAsJsonAsync(
            $"/api/equipment/{equipment.Id}/status",
            new UpdateEquipmentStatusRequest("Maintenance"));

        Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        var updated = await patched.Content.ReadFromJsonAsync<EquipmentDto>();
        Assert.Equal("Maintenance", updated!.Status);

        var loaded = await client.GetFromJsonAsync<EquipmentDto>($"/api/equipment/{equipment.Id}");
        Assert.Equal("Maintenance", loaded!.Status);
    }

    [Fact]
    public async Task EquipmentListFiltersBySearchAndCategory()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var byName = await client.GetFromJsonAsync<PagedResult<EquipmentDto>>(
            "/api/equipment?search=hammer");
        Assert.NotNull(byName);
        Assert.Single(byName.Items);
        Assert.Equal("Rotary hammer", byName.Items[0].Name);

        var byCategory = await client.GetFromJsonAsync<PagedResult<EquipmentDto>>(
            "/api/equipment?categoryId=2");
        Assert.NotNull(byCategory);
        Assert.Equal(2, byCategory.TotalCount);
    }

    [Fact]
    public async Task InvalidRequestsReturnBadRequestOrNotFound()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var negativeRate = await client.PostAsJsonAsync(
            "/api/equipment",
            new CreateEquipmentRequest(1, "Invalid rate", -1m, null));
        Assert.Equal(HttpStatusCode.BadRequest, negativeRate.StatusCode);

        var missingCategory = await client.PostAsJsonAsync(
            "/api/equipment",
            new CreateEquipmentRequest(9999, "Missing category", 10m, null));
        Assert.Equal(HttpStatusCode.NotFound, missingCategory.StatusCode);

        var invalidStatus = await client.PatchAsJsonAsync(
            "/api/equipment/1/status", new UpdateEquipmentStatusRequest("Borrowed"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/categories/9999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync(
                "/api/categories/9999", new UpdateCategoryRequest("Ghost", null))).StatusCode);
    }

    [Fact]
    public async Task AdminCanRenameCategoryAndEquipment()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var category = await client.PutAsJsonAsync(
            "/api/categories/1", new UpdateCategoryRequest("Hand tools", "Renamed"));

        Assert.Equal(HttpStatusCode.OK, category.StatusCode);

        var equipment = await client.PutAsJsonAsync(
            "/api/equipment/1",
            new UpdateEquipmentRequest("Renamed wrench", 90m, "Reworked"));

        Assert.Equal(HttpStatusCode.OK, equipment.StatusCode);
        var body = await equipment.Content.ReadFromJsonAsync<EquipmentDto>();
        Assert.Equal("Renamed wrench", body!.Name);
        Assert.Equal(90m, body.DailyRate);
    }
}
