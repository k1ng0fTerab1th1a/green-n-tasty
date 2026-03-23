using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Core.Errors;
using System.Net;
using System.Text.Json;

namespace Restaurant.IntegrationTests.Api;

public sealed class BookingEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public BookingEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task GetAvailableTables_WithoutAuthHeader_ShouldReturn200()
    {
        _factory.TableService.Reset();

        var res = await _client.GetAsync("/bookings/tables?date=2026-06-01");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Date validation (controller level) ──────────────────────────────────

    [Fact]
    public async Task GetAvailableTables_WithInvalidDateFormat_ShouldReturn400WithMessage()
    {
        _factory.TableService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/bookings/tables?date=01-06-2026"));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString()
            .Should().Be("Date must be in yyyy-MM-dd format.");
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableTables_WithValidDate_ShouldReturn200AndMapAllFields()
    {
        _factory.TableService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/bookings/tables?date=2026-06-01"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(1);

        var item = data[0];
        item.GetPropertyIgnoreCase("locationId").GetString().Should().Be("loc-1");
        item.GetPropertyIgnoreCase("tableNumber").GetInt32().Should().Be(3);
        item.GetPropertyIgnoreCase("locationAddress").GetString().Should().Be("Main street 1");
        item.GetPropertyIgnoreCase("capacity").GetInt32().Should().Be(4);

        var slots = item.GetPropertyIgnoreCase("availableSlots");
        slots.ValueKind.Should().Be(JsonValueKind.Array);
        slots.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetAvailableTables_WhenNoTablesAvailable_ShouldReturn200WithEmptyArray()
    {
        _factory.TableService.Reset();
        _factory.TableService.Response.Clear();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/bookings/tables?date=2026-06-01"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(0);
    }

    // ─── Optional query params forwarded to service ───────────────────────────

    [Fact]
    public async Task GetAvailableTables_WithAllOptionalParams_ShouldPassThemToService()
    {
        _factory.TableService.Reset();

        await _client.SendAsync(Authed(HttpMethod.Get, "/bookings/tables?date=2026-06-01&time=14:00&locationId=loc-1&guests=4"));

        _factory.TableService.LastDate.Should().Be(new DateOnly(2026, 6, 1));
        _factory.TableService.LastTime.Should().Be(new TimeOnly(14, 0));
        _factory.TableService.LastLocationId.Should().Be("loc-1");
        _factory.TableService.LastCapacity.Should().Be(4);
    }

    [Fact]
    public async Task GetAvailableTables_WithoutOptionalParams_ShouldPassNullsToService()
    {
        _factory.TableService.Reset();

        await _client.SendAsync(Authed(HttpMethod.Get, "/bookings/tables?date=2026-06-01"));

        _factory.TableService.LastDate.Should().Be(new DateOnly(2026, 6, 1));
        _factory.TableService.LastTime.Should().BeNull();
        _factory.TableService.LastLocationId.Should().BeNull();
        _factory.TableService.LastCapacity.Should().BeNull();
    }

    // ─── Failed Result mapped to 400 by the controller ──────────────────────

    [Fact]
    public async Task GetAvailableTables_WhenServiceReturnsValidationError_ShouldReturn400WithMessage()
    {
        _factory.TableService.Reset();
        _factory.TableService.FailResult = new BusinessError("Cannot find available slots in the past.", ErrorType.Validation);

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/bookings/tables?date=2026-06-01"));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString()
            .Should().Be("Cannot find available slots in the past.");
    }
}
