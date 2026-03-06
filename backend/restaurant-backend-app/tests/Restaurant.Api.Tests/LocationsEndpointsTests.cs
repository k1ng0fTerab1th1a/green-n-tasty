using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;

namespace Restaurant.Api.Tests
{
    public sealed class LocationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public LocationsEndpointsTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetLocations_ShouldReturn200_AndMapFields()
        {
            var res = await _client.GetAsync("/locations");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

            var data = doc.RootElement.GetPropertyIgnoreCase("data");
            data.ValueKind.Should().Be(JsonValueKind.Array);
            data.GetArrayLength().Should().Be(1);

            var item = data[0];
            item.GetPropertyIgnoreCase("id").GetString().Should().Be("loc-1");
            item.GetPropertyIgnoreCase("totalCapacity").GetString().Should().Be("120");
            item.GetPropertyIgnoreCase("averageOccupancy").GetString().Should().Be("35%");
            item.GetPropertyIgnoreCase("rating").GetString().Should().Be("4.6");
        }

        [Fact]
        public async Task GetLocationSelectOptions_ShouldReturn200()
        {
            var res = await _client.GetAsync("/locations/select-options");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var data = doc.RootElement.GetPropertyIgnoreCase("data");
            data.ValueKind.Should().Be(JsonValueKind.Array);
            data.GetArrayLength().Should().Be(1);
            data[0].GetPropertyIgnoreCase("id").GetString().Should().Be("loc-1");
            data[0].GetPropertyIgnoreCase("address").GetString().Should().Be("Main street 1");
        }

        [Fact]
        public async Task GetFeedbacks_WithoutType_ShouldReturn400()
        {
            var res = await _client.GetAsync("/locations/loc-1/feedbacks");
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetFeedbacks_WithType_ShouldReturn200()
        {
            var res = await _client.GetAsync("/locations/loc-1/feedbacks?type=GENERAL");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

            var data = doc.RootElement.GetPropertyIgnoreCase("data");
            data.GetPropertyIgnoreCase("size").GetInt32().Should().Be(20);
            data.GetPropertyIgnoreCase("content").GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task GetSpecialityDishes_ShouldReturn200()
        {
            var res = await _client.GetAsync("/locations/loc-1/speciality-dishes");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
            doc.RootElement.GetPropertyIgnoreCase("data").GetArrayLength().Should().Be(0);
        }
    }
}
