using FluentAssertions;
using Restaurant.Api.Tests;
using System.Net;
using System.Text;
using System.Net.Http.Json;
using System.Text.Json;

namespace Restaurant.IntegrationTests.Api;

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
        item.GetPropertyIgnoreCase("customerName").GetString().Should().Be("Anna Smith");
        item.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Walter One");
        item.GetPropertyIgnoreCase("locationId").GetString().Should().Be("loc-1");
        item.GetPropertyIgnoreCase("locationAddress").GetString().Should().Be("Main street 1");
        item.GetPropertyIgnoreCase("tableNumber").GetInt32().Should().Be(3);
        item.GetPropertyIgnoreCase("guestsCount").GetInt32().Should().Be(2);
        item.GetPropertyIgnoreCase("startDateTime").GetString().Should().Be("2026-03-05T10:00:00.0000000Z");
        item.GetPropertyIgnoreCase("endDateTime").GetString().Should().Be("2026-03-05T11:30:00.0000000Z");
        item.GetPropertyIgnoreCase("status").GetString().Should().Be("Reserved");
        item.GetPropertyIgnoreCase("actualStartTime").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetPropertyIgnoreCase("actualEndTime").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetPropertyIgnoreCase("isCreatedByWaiter").GetBoolean().Should().BeFalse();
        item.GetPropertyIgnoreCase("visitorName").ValueKind.Should().Be(JsonValueKind.Null);
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
        dto.GetPropertyIgnoreCase("customerName").GetString().Should().Be("Anna Smith");
        dto.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Walter One");
        dto.GetPropertyIgnoreCase("locationId").GetString().Should().Be("loc-1");
        dto.GetPropertyIgnoreCase("locationAddress").GetString().Should().Be("Main street 1");
        dto.GetPropertyIgnoreCase("tableNumber").GetInt32().Should().Be(3);
        dto.GetPropertyIgnoreCase("guestsCount").GetInt32().Should().Be(2);
        dto.GetPropertyIgnoreCase("startDateTime").GetString().Should().Be("2026-03-05T10:00:00.0000000Z");
        dto.GetPropertyIgnoreCase("endDateTime").GetString().Should().Be("2026-03-05T11:30:00.0000000Z");
        dto.GetPropertyIgnoreCase("status").GetString().Should().Be("Reserved");
        dto.GetPropertyIgnoreCase("isCreatedByWaiter").GetBoolean().Should().BeFalse();
        dto.GetPropertyIgnoreCase("visitorName").ValueKind.Should().Be(JsonValueKind.Null);
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
        dto.GetPropertyIgnoreCase("customerName").GetString().Should().Be("John Doe");
        dto.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Walter One");
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

    [Fact]
    public async Task SearchCustomersForWaiter_WithoutUserHeader_ShouldReturn401()
    {
        _factory.ReservationService.Reset();

        var res = await _client.GetAsync("/reservations/waiter/customers?query=Anna");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchCustomersForWaiter_AsWaiter_ShouldReturn200()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/waiter/customers?query=Anna", userId: "waiter-1", role: "WAITER"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(1);

        var item = data[0];
        item.GetPropertyIgnoreCase("customerId").GetString().Should().Be("customer-1");
        item.GetPropertyIgnoreCase("username").GetString().Should().Be("Anna Smith");
        item.GetPropertyIgnoreCase("maskedEmail").GetString().Should().Be("a**a@example.com");

        _factory.ReservationService.LastSearchActorUserId.Should().Be("waiter-1");
        _factory.ReservationService.LastSearchQuery.Should().Be("Anna");
    }

    [Fact]
    public async Task SearchCustomersForWaiter_AsNonWaiter_ShouldReturn403()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/waiter/customers?query=Anna", userId: "customer-1", role: "CUSTOMER"));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Forbidden.");
    }

    [Fact]
    public async Task CreateForWaiter_WithoutUserHeader_ShouldReturn401()
    {
        _factory.ReservationService.Reset();

        var res = await _client.PostAsJsonAsync("/reservations/waiter", new
        {
            locationId = "loc-1",
            tableNumber = 3,
            date = "2030-03-05",
            timeFrom = "12:00",
            timeTo = "13:00",
            guestsCount = 2,
            customerId = "customer-1"
        });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateForWaiter_AsWaiter_ForExistingCustomer_ShouldReturn201()
    {
        _factory.ReservationService.Reset();

        var request = Authed(HttpMethod.Post, "/reservations/waiter", userId: "waiter-1", role: "WAITER");
        request.Content = JsonContent.Create(new
        {
            locationId = "loc-1",
            tableNumber = 3,
            date = "2030-03-05",
            timeFrom = "12:00",
            timeTo = "13:00",
            guestsCount = 2,
            customerId = "customer-1",
            visitorName = (string?)null
        });

        var res = await _client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("locationId").GetString().Should().Be("loc-1");
        data.GetPropertyIgnoreCase("customerName").GetString().Should().Be("Customer customer-1");
        data.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Waiter waiter-1");
        data.GetPropertyIgnoreCase("locationAddress").GetString().Should().Be("Main street 1");
        data.GetPropertyIgnoreCase("tableNumber").GetInt32().Should().Be(3);
        data.GetPropertyIgnoreCase("guestsCount").GetInt32().Should().Be(2);
        data.GetPropertyIgnoreCase("actualStartTime").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetPropertyIgnoreCase("actualEndTime").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetPropertyIgnoreCase("isCreatedByWaiter").GetBoolean().Should().BeTrue();
        data.GetPropertyIgnoreCase("visitorName").ValueKind.Should().Be(JsonValueKind.Null);

        _factory.ReservationService.LastCreateForWaiterActorUserId.Should().Be("waiter-1");
        _factory.ReservationService.LastCreateForWaiterDto.Should().NotBeNull();
        _factory.ReservationService.LastCreateForWaiterDto!.CustomerId.Should().Be("customer-1");
        _factory.ReservationService.LastCreateForWaiterDto!.VisitorName.Should().BeNull();
    }

    [Fact]
    public async Task CreateForWaiter_AsWaiter_ForAnonymousVisitor_ShouldReturn201()
    {
        _factory.ReservationService.Reset();

        var request = Authed(HttpMethod.Post, "/reservations/waiter", userId: "waiter-1", role: "WAITER");
        request.Content = JsonContent.Create(new
        {
            locationId = "loc-1",
            tableNumber = 3,
            date = "2030-03-05",
            timeFrom = "12:00",
            timeTo = "13:00",
            guestsCount = 2,
            customerId = (string?)null,
            visitorName = "Anna Visitor"
        });

        var res = await _client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("customerName").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Waiter waiter-1");
        data.GetPropertyIgnoreCase("actualStartTime").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetPropertyIgnoreCase("actualEndTime").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetPropertyIgnoreCase("isCreatedByWaiter").GetBoolean().Should().BeTrue();
        data.GetPropertyIgnoreCase("visitorName").GetString().Should().Be("Anna Visitor");

        _factory.ReservationService.LastCreateForWaiterActorUserId.Should().Be("waiter-1");
        _factory.ReservationService.LastCreateForWaiterDto.Should().NotBeNull();
        _factory.ReservationService.LastCreateForWaiterDto!.CustomerId.Should().BeNull();
        _factory.ReservationService.LastCreateForWaiterDto!.VisitorName.Should().Be("Anna Visitor");
    }

    [Fact]
    public async Task CreateForWaiter_AsNonWaiter_ShouldReturn403()
    {
        _factory.ReservationService.Reset();

        var request = Authed(HttpMethod.Post, "/reservations/waiter", userId: "customer-1", role: "CUSTOMER");
        request.Content = JsonContent.Create(new
        {
            locationId = "loc-1",
            tableNumber = 3,
            date = "2030-03-05",
            timeFrom = "12:00",
            timeTo = "13:00",
            guestsCount = 2,
            customerId = "customer-1"
        });

        var res = await _client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Forbidden.");
    }

    [Fact]
    public async Task Delete_AsOwner_ShouldReturn200_AndSuccessMessage()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Delete, "/reservations/r-customer-1", userId: "customer-1"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ReservationService.LastCancelReservationId.Should().Be("r-customer-1");
        _factory.ReservationService.LastCancelUserId.Should().Be("customer-1");
        _factory.ReservationService.LastCancelIsWaiter.Should().BeFalse();

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Reservation cancelled successfully.");
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsFalse_ShouldReturn500()
    {
        _factory.ReservationService.Reset();
        _factory.ReservationService.CancelShouldSucceed = false;

        var res = await _client.SendAsync(Authed(HttpMethod.Delete, "/reservations/r-customer-1", userId: "customer-1"));

        res.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("During reservation cancellation something went wrong");
    }

    [Fact]
    public async Task CreateForClient_WithoutUserHeader_ShouldReturn401()
    {
        _factory.ReservationService.Reset();

        var payload = """
        {
          "locationId": "loc-10",
          "tableNumber": 4,
          "date": "2026-04-01",
          "timeFrom": "12:00",
          "timeTo": "13:30",
          "guestsCount": 3
        }
        """;

        var req = new HttpRequestMessage(HttpMethod.Post, "/reservations/client")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateForClient_WithValidPayload_ShouldReturn201_AndMapAllFields()
    {
        _factory.ReservationService.Reset();

        var payload = """
        {
          "locationId": "loc-10",
          "tableNumber": 4,
          "date": "2026-04-01",
          "timeFrom": "12:00",
          "timeTo": "13:30",
          "guestsCount": 3
        }
        """;

        var req = Authed(HttpMethod.Post, "/reservations/client", userId: "customer-77");
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);

        _factory.ReservationService.LastCreateCustomerId.Should().Be("customer-77");
        _factory.ReservationService.LastCreateDto.Should().NotBeNull();
        _factory.ReservationService.LastCreateDto!.LocationId.Should().Be("loc-10");
        _factory.ReservationService.LastCreateDto.TableNumber.Should().Be(4);
        _factory.ReservationService.LastCreateDto.GuestsCount.Should().Be(3);
        _factory.ReservationService.LastCreateDto.Date.ToString("yyyy-MM-dd").Should().Be("2026-04-01");
        _factory.ReservationService.LastCreateDto.TimeFrom.ToString("HH:mm").Should().Be("12:00");
        _factory.ReservationService.LastCreateDto.TimeTo.ToString("HH:mm").Should().Be("13:30");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("id").GetString().Should().Be("r-created-1");
        data.GetPropertyIgnoreCase("customerName").GetString().Should().Be("Customer customer-77");
        data.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Auto Waiter");
        data.GetPropertyIgnoreCase("locationId").GetString().Should().Be("loc-10");
        data.GetPropertyIgnoreCase("tableNumber").GetInt32().Should().Be(4);
        data.GetPropertyIgnoreCase("guestsCount").GetInt32().Should().Be(3);
        data.GetPropertyIgnoreCase("status").GetString().Should().Be("Reserved");
        data.GetPropertyIgnoreCase("actualStartTime").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetPropertyIgnoreCase("actualEndTime").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetPropertyIgnoreCase("startDateTime").GetString().Should().Be("2026-04-01T12:00:00.0000000Z");
        data.GetPropertyIgnoreCase("endDateTime").GetString().Should().Be("2026-04-01T13:30:00.0000000Z");
    }


    [Fact]
    public async Task StartReservation_WithoutUserHeader_ShouldReturn401()
    {
        _factory.ReservationService.Reset();

        var res = await _client.PostAsync("/reservations/r-customer-1/start", content: null);

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StartReservation_AsNonWaiter_ShouldReturn403()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Post, "/reservations/r-customer-1/start", userId: "customer-1", role: "CUSTOMER"));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Forbidden.");
    }

    [Fact]
    public async Task StartReservation_AsAssignedWaiter_ShouldReturn200_AndSetActualStartTime()
    {
        _factory.ReservationService.Reset();

        var res = await _client.SendAsync(Authed(HttpMethod.Post, "/reservations/r-customer-1/start", userId: "waiter-1", role: "WAITER"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ReservationService.LastLifecycleReservationId.Should().Be("r-customer-1");
        _factory.ReservationService.LastLifecycleWaiterId.Should().Be("waiter-1");
        _factory.ReservationService.LastLifecycleAction.Should().Be("start");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("status").GetString().Should().Be("InProgress");
        data.GetPropertyIgnoreCase("actualStartTime").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetPropertyIgnoreCase("actualEndTime").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task MarkMealsServed_AsAssignedWaiter_ShouldReturn200_AndSetMealsServedStatus()
    {
        _factory.ReservationService.Reset();

        var reservation = _factory.ReservationService.SeedReservations.Single(x => x.Id == "r-customer-1");
        reservation.Status = Restaurant.Core.Models.ReservationStatus.InProgress;
        reservation.ActualStartTime = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("O");

        var res = await _client.SendAsync(Authed(HttpMethod.Post, "/reservations/r-customer-1/meals-served", userId: "waiter-1", role: "WAITER"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ReservationService.LastLifecycleAction.Should().Be("meals-served");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("status").GetString().Should().Be("MealsServed");
        data.GetPropertyIgnoreCase("actualStartTime").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetPropertyIgnoreCase("actualEndTime").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task FinishReservation_AsAssignedWaiter_ShouldReturn200_AndSetActualEndTime()
    {
        _factory.ReservationService.Reset();

        var reservation = _factory.ReservationService.SeedReservations.Single(x => x.Id == "r-customer-1");
        reservation.Status = Restaurant.Core.Models.ReservationStatus.MealsServed;
        reservation.ActualStartTime = DateTimeOffset.UtcNow.AddHours(-1).ToString("O");

        var res = await _client.SendAsync(Authed(HttpMethod.Post, "/reservations/r-customer-1/finish", userId: "waiter-1", role: "WAITER"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ReservationService.LastLifecycleAction.Should().Be("finish");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("status").GetString().Should().Be("Finished");
        data.GetPropertyIgnoreCase("actualStartTime").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetPropertyIgnoreCase("actualEndTime").GetString().Should().NotBeNullOrWhiteSpace();
    }

}
