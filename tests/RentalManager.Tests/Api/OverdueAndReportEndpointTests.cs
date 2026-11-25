using System.Net;
using System.Net.Http.Json;
using RentalManager.Application.Contracts;

namespace RentalManager.Tests.Api;

public class OverdueAndReportEndpointTests : IClassFixture<ApiFactory>
{
    private static readonly DateTime AsOf = new(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);

    private readonly ApiFactory _factory;

    public OverdueAndReportEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static CreateReservationRequest Request(
        DateTime from, DateTime to, params CreateReservationLineRequest[] lines) =>
        new(1, from, to, null, lines);

    private static CreateReservationLineRequest Line(
        int equipmentId, DateTime from, DateTime to, int quantity = 1) =>
        new(equipmentId, from, to, quantity);

    private static async Task<ReservationDto> CreateActiveAsync(
        HttpClient client, DateTime from, DateTime to, params CreateReservationLineRequest[] lines)
    {
        var created = await client.PostAsJsonAsync("/api/reservations", Request(from, to, lines));
        var reservation = await created.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(reservation);

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/reservations/{reservation.Id}/start", null)).StatusCode);

        return reservation;
    }

    private static string SweepUrl() =>
        $"/api/reservations/mark-overdue?now={Uri.EscapeDataString(AsOf.ToString("O"))}";

    [Fact]
    public async Task SweepMarksOnlyExpiredActiveReservations()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var expired = await CreateActiveAsync(client,
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc),
            Line(2, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc)));

        var current = await CreateActiveAsync(client,
            new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            Line(3, new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc)));

        var first = await client.PostAsync(SweepUrl(), null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var result = await first.Content.ReadFromJsonAsync<OverdueSweepDto>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Marked);

        var overdue = await client.GetFromJsonAsync<PagedResult<ReservationDto>>(
            "/api/reservations?status=Overdue");
        Assert.NotNull(overdue);
        Assert.Equal(expired.Id, Assert.Single(overdue.Items).Id);

        var active = await client.GetFromJsonAsync<PagedResult<ReservationDto>>(
            "/api/reservations?status=Active");
        Assert.NotNull(active);
        Assert.Equal(current.Id, Assert.Single(active.Items).Id);

        var second = await client.PostAsync(SweepUrl(), null);
        var repeated = await second.Content.ReadFromJsonAsync<OverdueSweepDto>();
        Assert.NotNull(repeated);
        Assert.Equal(0, repeated.Marked);
    }

    [Fact]
    public async Task SweepRequiresManagerRole()
    {
        var staff = ApiClientHelpers.StaffClient(_factory);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await staff.PostAsync(SweepUrl(), null)).StatusCode);

        var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsync(SweepUrl(), null)).StatusCode);
    }

    [Fact]
    public async Task OverdueBookingStillBlocksStock()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var from = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);

        await CreateActiveAsync(client, from, to, Line(2, from, to, 2));
        await client.PostAsync(SweepUrl(), null);

        var availability = await client.GetFromJsonAsync<AvailabilityResultDto>(
            "/api/reservations/availability?from=2026-11-05&to=2026-11-08");
        Assert.NotNull(availability);
        var hammer = availability.Items.Single(x => x.EquipmentId == 2);
        Assert.Equal(2, hammer.Booked);
        Assert.Equal(1, hammer.Available);

        var overlapping = await client.PostAsJsonAsync("/api/reservations",
            Request(new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc),
                Line(2, new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc), 2)));
        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);

        var adjacent = await client.PostAsJsonAsync("/api/reservations",
            Request(to, new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
                Line(2, to, new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc))));
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
    }

    [Fact]
    public async Task MonthlyReportAggregatesRevenueAndOverdueBookings()
    {
        var client = await ApiClientHelpers.AdminClientAsync(_factory);

        var lateFrom = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);
        var lateTo = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);
        var created = await client.PostAsJsonAsync("/api/reservations",
            Request(lateFrom, lateTo,
                Line(2, lateFrom, lateTo),
                Line(5, lateFrom, lateTo, 2)));
        var late = await created.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(late);
        await client.PostAsync($"/api/reservations/{late.Id}/confirm", null);
        await client.PostAsync($"/api/reservations/{late.Id}/start", null);

        var doneFrom = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var doneTo = new DateTime(2026, 10, 17, 0, 0, 0, DateTimeKind.Utc);
        var doneCreated = await client.PostAsJsonAsync("/api/reservations",
            Request(doneFrom, doneTo, Line(1, doneFrom, doneTo)));
        var done = await doneCreated.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(done);
        await client.PostAsync($"/api/reservations/{done.Id}/confirm", null);
        await client.PostAsync($"/api/reservations/{done.Id}/start", null);
        await client.PostAsync($"/api/reservations/{done.Id}/complete", null);

        var draftCreated = await client.PostAsJsonAsync("/api/reservations",
            Request(new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc),
                Line(1, new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc))));
        var draft = await draftCreated.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(draft);

        var cancelledCreated = await client.PostAsJsonAsync("/api/reservations",
            Request(new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc),
                Line(1, new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc))));
        var cancelled = await cancelledCreated.Content.ReadFromJsonAsync<ReservationDto>();
        Assert.NotNull(cancelled);
        await client.PostAsync($"/api/reservations/{cancelled.Id}/confirm", null);
        await client.PostAsync($"/api/reservations/{cancelled.Id}/cancel", null);

        var asOf = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);
        var swept = await client.PostAsync(
            $"/api/reservations/mark-overdue?now={Uri.EscapeDataString(asOf.ToString("O"))}", null);
        Assert.Equal(HttpStatusCode.OK, swept.StatusCode);

        var report = await client.GetFromJsonAsync<MonthlyReportDto>(
            $"/api/reports/monthly?year=2026&month=10&asOf={Uri.EscapeDataString(asOf.ToString("O"))}");
        Assert.NotNull(report);

        Assert.Equal(2, report.ReservationCount);
        Assert.Equal(1, report.CompletedCount);
        Assert.Equal(1, report.OverdueCount);
        Assert.Equal(494m, report.TotalRevenue);
        Assert.Equal(11, report.RentalDays);
        Assert.Equal(2, report.ByCategory.Count);
        Assert.Equal("Power tools", report.ByCategory[0].Category);
        Assert.Equal(455m, report.ByCategory[0].Revenue);
        Assert.Equal("Scaffolding", report.ByCategory[1].Category);
        Assert.Equal(39m, report.ByCategory[1].Revenue);

        var overdue = Assert.Single(report.Overdue);
        Assert.Equal(late.Reference, overdue.Reference);
        Assert.Equal(43, overdue.DaysOverdue);
        Assert.Equal(324m, overdue.TotalAmount);

        var empty = await client.GetFromJsonAsync<MonthlyReportDto>(
            "/api/reports/monthly?year=2026&month=9");
        Assert.NotNull(empty);
        Assert.Equal(0, empty.ReservationCount);
        Assert.Equal(0m, empty.TotalRevenue);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/reports/monthly?year=2026&month=13")).StatusCode);
    }
}
