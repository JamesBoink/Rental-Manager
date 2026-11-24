using System.Net;
using System.Net.Http.Json;
using RentalManager.Application.Contracts;

namespace RentalManager.Tests.Api;

public class ReservationEndpointTests : IClassFixture<ApiFactory>
{
    private static readonly DateTime From = new(2026, 12, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = new(2026, 12, 14, 0, 0, 0, DateTimeKind.Utc);

    private readonly ApiFactory _factory;

    public ReservationEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static CreateReservationRequest Request(
        int customerId = 1,
        DateTime? from = null,
        DateTime? to = null,
        IReadOnlyList<CreateReservationLineRequest>? lines = null) =>
        new(customerId, from ?? From, to ?? To, null,
            lines ?? Array.Empty<CreateReservationLineRequest>());

    private static CreateReservationLineRequest Line(
        int equipmentId, int quantity = 1, DateTime? from = null, DateTime? to = null) =>
        new(equipmentId, from ?? From, to ?? To, quantity);

    [Fact]
    public async Task AnonymousCallersCannotReadReservations()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/reservations/availability?from=2026-12-10&to=2026-12-14")).StatusCode);
    }

    [Fact]
    public async Task CreateCalculatesTotalAndStartsAsDraft()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/reservations",
            Request(lines: new[]
            {
                Line(2, 2, From, From.AddDays(2)),
                Line(3, 1, From, From.AddDays(3))
            }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();

        Assert.NotNull(reservation);
        Assert.Equal("Draft", reservation.Status);
        Assert.StartsWith("R-2026-", reservation.Reference);
        Assert.Equal(2, reservation.Lines.Count);
        Assert.Equal(95m, reservation.Lines[0].UnitPrice);
        Assert.Equal(515m, reservation.TotalAmount);
        Assert.Equal("Buildtech Sp. z o.o.", reservation.CustomerName);
    }

    [Fact]
    public async Task ConfirmedBookingBlocksOverlapButNotAdjacentPeriods()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var first = await client.PostAsJsonAsync("/api/reservations",
            Request(
                from: new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2026, 12, 4, 0, 0, 0, DateTimeKind.Utc),
                lines: new[]
                {
                    Line(2, 2,
                        new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 12, 4, 0, 0, 0, DateTimeKind.Utc))
                }));

        var firstBody = await first.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(firstBody);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/reservations/{firstBody.Id}/confirm", null)).StatusCode);

        var availability = await client.GetFromJsonAsync<AvailabilityResultDto>(
            "/api/reservations/availability?from=2026-12-01&to=2026-12-04");
        Assert.NotNull(availability);
        var hammer = availability.Items.Single(x => x.EquipmentId == 2);
        Assert.Equal(3, hammer.Stock);
        Assert.Equal(2, hammer.Booked);
        Assert.Equal(1, hammer.Available);

        var overlapping = await client.PostAsJsonAsync("/api/reservations",
            Request(
                from: new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2026, 12, 5, 0, 0, 0, DateTimeKind.Utc),
                lines: new[]
                {
                    Line(2, 2,
                        new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 12, 5, 0, 0, 0, DateTimeKind.Utc))
                }));
        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);

        var adjacent = await client.PostAsJsonAsync("/api/reservations",
            Request(
                from: new DateTime(2026, 12, 4, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2026, 12, 7, 0, 0, 0, DateTimeKind.Utc),
                lines: new[]
                {
                    Line(2, 3,
                        new DateTime(2026, 12, 4, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 12, 7, 0, 0, 0, DateTimeKind.Utc))
                }));
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
    }

    [Fact]
    public async Task DraftsDoNotBlockAvailability()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var draft = await client.PostAsJsonAsync("/api/reservations",
            Request(
                from: new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2026, 12, 22, 0, 0, 0, DateTimeKind.Utc),
                lines: new[]
                {
                    Line(2, 3,
                        new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 12, 22, 0, 0, 0, DateTimeKind.Utc))
                }));
        Assert.Equal(HttpStatusCode.Created, draft.StatusCode);

        var availability = await client.GetFromJsonAsync<AvailabilityResultDto>(
            "/api/reservations/availability?from=2026-12-20&to=2026-12-22");
        Assert.NotNull(availability);
        Assert.Equal(0, availability.Items.Single(x => x.EquipmentId == 2).Booked);
    }

    [Fact]
    public async Task StatusTransitionsAreEnforced()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/reservations",
            Request(
                from: new DateTime(2027, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2027, 1, 7, 0, 0, 0, DateTimeKind.Utc),
                lines: new[]
                {
                    Line(1, 1,
                        new DateTime(2027, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2027, 1, 7, 0, 0, 0, DateTimeKind.Utc))
                }));
        var reservation = await response.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(reservation);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/complete", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsync("/api/reservations/9999/confirm", null)).StatusCode);
    }

    [Fact]
    public async Task LinesCanOnlyBeAddedToDrafts()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var created = await client.PostAsJsonAsync("/api/reservations",
            Request(
                from: new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                to: new DateTime(2027, 2, 5, 0, 0, 0, DateTimeKind.Utc),
                lines: new[]
                {
                    Line(1, 1,
                        new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2027, 2, 5, 0, 0, 0, DateTimeKind.Utc))
                }));
        var reservation = await created.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(reservation);

        var added = await client.PostAsJsonAsync($"/api/reservations/{reservation.Id}/lines",
            Line(3, 1,
                new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2027, 2, 3, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var updated = await added.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.Equal(2, updated!.Lines.Count);
        Assert.Equal(430m, updated.TotalAmount);

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync($"/api/reservations/{reservation.Id}/lines", Line(4))).StatusCode);
    }

    [Fact]
    public async Task InvalidRequestsReturnBadRequestOrNotFound()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var noLines = await client.PostAsJsonAsync("/api/reservations", Request());
        Assert.Equal(HttpStatusCode.BadRequest, noLines.StatusCode);

        var outsideWindow = await client.PostAsJsonAsync("/api/reservations",
            Request(lines: new[] { Line(1, 1, From.AddDays(-1), To) }));
        Assert.Equal(HttpStatusCode.BadRequest, outsideWindow.StatusCode);

        var missingCustomer = await client.PostAsJsonAsync("/api/reservations",
            Request(customerId: 9999, lines: new[] { Line(1) }));
        Assert.Equal(HttpStatusCode.NotFound, missingCustomer.StatusCode);

        var missingEquipment = await client.PostAsJsonAsync("/api/reservations",
            Request(lines: new[] { Line(9999) }));
        Assert.Equal(HttpStatusCode.NotFound, missingEquipment.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/reservations/9999")).StatusCode);
    }

    [Fact]
    public async Task ReservationListFiltersByStatus()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var result = await client.GetFromJsonAsync<PagedResult<ReservationDto>>(
            "/api/reservations?status=Draft&pageSize=100");

        Assert.NotNull(result);
        Assert.All(result.Items, x => Assert.Equal("Draft", x.Status));
    }
}
