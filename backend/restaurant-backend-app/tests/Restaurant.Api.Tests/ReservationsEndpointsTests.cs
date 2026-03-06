using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace Restaurant.Api.Tests
{
    public sealed class ReservationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ReservationsEndpointsTests(CustomWebApplicationFactory factory)
        {
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
            var res = await _client.GetAsync("/reservations");
            res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMy_AsCustomer_ShouldReturn200_WithItems()
        {
            var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations", userId: "customer-1"));
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

            var data = doc.RootElement.GetPropertyIgnoreCase("data");
            data.ValueKind.Should().Be(JsonValueKind.Array);
            data.GetArrayLength().Should().Be(1);
            data[0].GetPropertyIgnoreCase("TableNumber").GetInt32().Should().Be(3);
        }

        [Fact]
        public async Task GetMy_AsWaiter_ShouldReturn200_WithDifferentTableNumber()
        {
            var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations", userId: "waiter-1", role: "WAITER"));
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var data = doc.RootElement.GetPropertyIgnoreCase("data");
            data.GetArrayLength().Should().Be(1);
            data[0].GetPropertyIgnoreCase("TableNumber").GetInt32().Should().Be(99);
        }

        [Fact]
        public async Task GetMy_WhenNoReservations_ShouldReturn200_WithEmptyArray()
        {
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
            var res = await _client.GetAsync("/reservations/r1");
            res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetById_WhenFound_ShouldReturn200()
        {
            var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/r1", userId: "customer-1"));
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

            var dto = doc.RootElement.GetPropertyIgnoreCase("data");
            dto.GetPropertyIgnoreCase("id").GetString().Should().Be("r1");
        }

        [Fact]
        public async Task GetById_WhenMissing_ShouldReturn404()
        {
            var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/missing", userId: "customer-1"));
            res.StatusCode.Should().Be(HttpStatusCode.NotFound);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
            doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Reservation not found.");
        }

        [Fact]
        public async Task GetById_WhenForbidden_ShouldReturn403()
        {
            var res = await _client.SendAsync(Authed(HttpMethod.Get, "/reservations/forbidden", userId: "customer-1"));
            res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
            doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Forbidden.");
        }
    }
}
