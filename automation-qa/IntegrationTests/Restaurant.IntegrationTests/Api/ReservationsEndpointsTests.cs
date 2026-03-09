using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace Restaurant.Api.Tests;

public sealed class ReservationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ReservationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string userId = "customer-1", string? role = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("X-User-Id", userId);
        if (!string.IsNullOrWhiteSpace(role))
            req.Headers.Add("X-Role", role);
        return req;
    }

    [Fact]
    public async Task GetMy_WithoutUserHeader_ShouldReturn401()
    {
        _factory.ReservationService.Reset();

        var res = await _client.GetAsync("/reservations");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMy_AsCustomer_ShouldReturn200_AndMapAllFields()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations", userId: "customer-1"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(1);

        var item = data[0];
        item.GetPropertyIgnoreCase("id").GetString().Should().Be("r-customer-1");
        item.GetPropertyIgnoreCase("locationId").GetString().Should().Be("loc-1");
        item.GetPropertyIgnoreCase("tableNumber").GetInt32().Should().Be(3);
        item.GetPropertyIgnoreCase("guestsCount").GetInt32().Should().Be(2);
        item.GetPropertyIgnoreCase("startDateTime").GetString().Should().Be("2026-03-05T10:00:00.0000000Z");
        item.GetPropertyIgnoreCase("endDateTime").GetString().Should().Be("2026-03-05T11:30:00.0000000Z");
        item.GetPropertyIgnoreCase("status").GetString().Should().Be("Reserved");
    }

    [Fact]
    public async Task GetMy_AsWaiter_ShouldReturn200_WithOnlyAssignedReservations()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations", userId: "waiter-1", role: "WAITER"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");

        data.GetArrayLength().Should().Be(2);

        var ids = data.EnumerateArray()
            .Select(x => x.GetPropertyIgnoreCase("id").GetString())
            .ToList();

        ids.Should().BeEquivalentTo(new[] { "r-customer-1", "r-customer-2" });
    }

    [Fact]
    public async Task GetMy_WhenNoReservations_ShouldReturn200_WithEmptyArray()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations", userId: "customer-empty"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetById_WithoutUserHeader_ShouldReturn401()
    {
        _factory.ReservationService.Reset();

        var res = await _client.GetAsync("/reservations/r-customer-1");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_AsOwner_ShouldReturn200_AndMapAllFields()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/r-customer-1", userId: "customer-1"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var dto = doc.RootElement.GetPropertyIgnoreCase("data");
        dto.GetPropertyIgnoreCase("id").GetString().Should().Be("r-customer-1");
        dto.GetPropertyIgnoreCase("locationId").GetString().Should().Be("loc-1");
        dto.GetPropertyIgnoreCase("tableNumber").GetInt32().Should().Be(3);
        dto.GetPropertyIgnoreCase("guestsCount").GetInt32().Should().Be(2);
        dto.GetPropertyIgnoreCase("startDateTime").GetString().Should().Be("2026-03-05T10:00:00.0000000Z");
        dto.GetPropertyIgnoreCase("endDateTime").GetString().Should().Be("2026-03-05T11:30:00.0000000Z");
        dto.GetPropertyIgnoreCase("status").GetString().Should().Be("Reserved");
    }

    [Fact]
    public async Task GetById_AsAssignedWaiter_ShouldReturn200()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/r-customer-2", userId: "waiter-1", role: "WAITER"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var dto = doc.RootElement.GetPropertyIgnoreCase("data");

        dto.GetPropertyIgnoreCase("id").GetString().Should().Be("r-customer-2");
        dto.GetPropertyIgnoreCase("tableNumber").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task GetById_WhenMissing_ShouldReturn404()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/missing", userId: "customer-1"));

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Reservation not found.");
    }

    [Fact]
    public async Task GetById_WhenForbiddenForDifferentWaiter_ShouldReturn403()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/r-customer-1", userId: "waiter-2", role: "WAITER"));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Forbidden.");
    }
}
