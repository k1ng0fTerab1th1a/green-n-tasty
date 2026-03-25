using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Core.Errors;
using Restaurant.Core.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Restaurant.IntegrationTests.Api;

public sealed class OrdersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public OrdersEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task CreateOrder_WithoutUserHeader_ShouldReturn401()
    {
        _factory.OrderService.Reset();

        var res = await _client.PostAsJsonAsync("/orders", new
        {
            reservationId = "r-customer-1",
            dishes = new[] { new { dishId = "dish-1", quantity = 1 } }
        });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.OrderService.LastActorId.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrder_AsNonWaiter_ShouldReturn403()
    {
        _factory.OrderService.Reset();

        var req = Authed(HttpMethod.Post, "/orders", userId: "customer-1", role: "CUSTOMER");
        req.Content = JsonContent.Create(new
        {
            reservationId = "r-customer-1",
            dishes = new[] { new { dishId = "dish-1", quantity = 1 } }
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _factory.OrderService.LastActorId.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrder_WhenDishesEmpty_ShouldReturn400_AndNotCallService()
    {
        _factory.OrderService.Reset();

        var req = Authed(HttpMethod.Post, "/orders", userId: "waiter-1", role: "WAITER");
        req.Content = JsonContent.Create(new
        {
            reservationId = "r-customer-1",
            dishes = Array.Empty<object>()
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.OrderService.LastActorId.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrder_WhenQuantityOutOfRange_ShouldReturn400_AndNotCallService()
    {
        _factory.OrderService.Reset();

        var req = Authed(HttpMethod.Post, "/orders", userId: "waiter-1", role: "WAITER");
        req.Content = JsonContent.Create(new
        {
            reservationId = "r-customer-1",
            dishes = new[] { new { dishId = "dish-1", quantity = 0 } }
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.OrderService.LastActorId.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrder_WhenServiceReturnsNotFound_ShouldReturn404()
    {
        _factory.OrderService.Reset();
        _factory.OrderService.CreateFailResult = OrderErrors.ReservationNotFound;

        var req = Authed(HttpMethod.Post, "/orders", userId: "waiter-1", role: "WAITER");
        req.Content = JsonContent.Create(new
        {
            reservationId = "missing-reservation",
            dishes = new[] { new { dishId = "dish-1", quantity = 1 } }
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Reservation not found.");

        _factory.OrderService.LastActorId.Should().Be("waiter-1");
        _factory.OrderService.LastDto.Should().NotBeNull();
        _factory.OrderService.LastDto!.ReservationId.Should().Be("missing-reservation");
    }

    [Fact]
    public async Task CreateOrder_WhenServiceReturnsConflict_ShouldReturn409()
    {
        _factory.OrderService.Reset();
        _factory.OrderService.CreateFailResult = OrderErrors.OrderAlreadyExists;

        var req = Authed(HttpMethod.Post, "/orders", userId: "waiter-1", role: "WAITER");
        req.Content = JsonContent.Create(new
        {
            reservationId = "r-customer-1",
            dishes = new[] { new { dishId = "dish-1", quantity = 1 } }
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Order already exists for this reservation.");
    }

    [Fact]
    public async Task CreateOrder_WhenValidPayload_ShouldReturn201_AndMapResponse()
    {
        _factory.OrderService.Reset();
        _factory.OrderService.CreateResponse = new Order
        {
            Id = "o-1",
            ReservationId = "r-customer-1",
            LocationId = "loc-1",
            LocationAddress = "Main street 1",
            WaiterId = "waiter-1",
            WaiterName = "Walter One",
            CustomerId = "customer-1",
            CustomerName = "Anna Smith",
            VisitorName = null,
            TableNumber = 3,
            GuestsCount = 2,
            Status = OrderStatus.Open,
            Dishes = new List<OrderDishSnapshot>
            {
                new()
                {
                    DishId = "dish-1",
                    Name = "Pasta",
                    Description = "Hot",
                    PhotoUrl = "img",
                    PriceAtOrder = 12.5f,
                    Quantity = 2
                }
            },
            TotalAmount = 25f,
            CreatedAt = "2026-03-01T00:00:00.0000000Z",
            CompletedAt = null
        };

        var req = Authed(HttpMethod.Post, "/orders", userId: "waiter-1", role: "WAITER");
        req.Content = JsonContent.Create(new
        {
            reservationId = "r-customer-1",
            dishes = new[]
            {
                new { dishId = "dish-1", quantity = 2 }
            }
        });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("id").GetString().Should().Be("o-1");
        data.GetPropertyIgnoreCase("reservationId").GetString().Should().Be("r-customer-1");
        data.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Walter One");
        data.GetPropertyIgnoreCase("guestsCount").GetInt32().Should().Be(2);
        data.GetPropertyIgnoreCase("status").GetString().Should().Be("Open");
        data.GetPropertyIgnoreCase("totalAmount").GetSingle().Should().Be(25f);

        var dishes = data.GetPropertyIgnoreCase("dishes");
        dishes.ValueKind.Should().Be(JsonValueKind.Array);
        dishes.GetArrayLength().Should().Be(1);
        dishes[0].GetPropertyIgnoreCase("dishId").GetString().Should().Be("dish-1");
        dishes[0].GetPropertyIgnoreCase("quantity").GetInt32().Should().Be(2);

        _factory.OrderService.LastActorId.Should().Be("waiter-1");
        _factory.OrderService.LastDto.Should().NotBeNull();
        _factory.OrderService.LastDto!.ReservationId.Should().Be("r-customer-1");
        _factory.OrderService.LastDto.Dishes.Should().HaveCount(1);
        _factory.OrderService.LastDto.Dishes[0].DishId.Should().Be("dish-1");
        _factory.OrderService.LastDto.Dishes[0].Quantity.Should().Be(2);
    }
}