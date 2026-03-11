using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;
using System.Net;
using System.Text.Json;

namespace Restaurant.IntegrationTests.Api;

public sealed class LocationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public LocationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLocations_ShouldReturn200_AndMapAllFields()
    {
        _factory.LocationService.Reset();
        _factory.LocationService.Locations.Clear();
        _factory.LocationService.Locations.Add(new Location
        {
            Id = "loc-42",
            Address = "Berlin, Test str 1",
            Description = "Panoramic hall",
            TotalCapacity = 120,
            AverageOccupancy = 0.354,
            ImageUrl = "http://img/loc-42",
            Rating = 4.64
        });

        var res = await _client.GetAsync("/locations");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(1);

        var item = data[0];
        item.GetPropertyIgnoreCase("id").GetString().Should().Be("loc-42");
        item.GetPropertyIgnoreCase("address").GetString().Should().Be("Berlin, Test str 1");
        item.GetPropertyIgnoreCase("description").GetString().Should().Be("Panoramic hall");
        item.GetPropertyIgnoreCase("totalCapacity").GetString().Should().Be("120");
        item.GetPropertyIgnoreCase("averageOccupancy").GetString().Should().Be("35%");
        item.GetPropertyIgnoreCase("imageUrl").GetString().Should().Be("http://img/loc-42");
        item.GetPropertyIgnoreCase("rating").GetString().Should().Be("4.6");
    }

    [Fact]
    public async Task GetLocations_WhenNoLocations_ShouldReturn200_WithEmptyArray()
    {
        _factory.LocationService.Reset();
        _factory.LocationService.Locations.Clear();

        var res = await _client.GetAsync("/locations");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetLocationSelectOptions_ShouldReturn200_AndMapFields()
    {
        _factory.LocationService.Reset();
        _factory.LocationService.Options.Clear();
        _factory.LocationService.Options.Add(new Location
        {
            Id = "loc-10",
            Address = "Main street 10",
            Description = "Ignored here",
            TotalCapacity = 50,
            AverageOccupancy = 0.4,
            ImageUrl = "http://img/10",
            Rating = 4.1
        });

        var res = await _client.GetAsync("/locations/select-options");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(1);
        data[0].GetPropertyIgnoreCase("id").GetString().Should().Be("loc-10");
        data[0].GetPropertyIgnoreCase("address").GetString().Should().Be("Main street 10");
    }

    [Fact]
    public async Task GetLocationSelectOptions_WhenNoItems_ShouldReturn200_WithEmptyArray()
    {
        _factory.LocationService.Reset();
        _factory.LocationService.Options.Clear();

        var res = await _client.GetAsync("/locations/select-options");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetFeedbacks_WithoutType_ShouldReturn400()
    {
        _factory.FeedbackService.Reset();

        var res = await _client.GetAsync("/locations/loc-1/feedbacks");

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeedbacks_WithTypeOnly_ShouldUseDefaultSort_AndReturnMappedResponse()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.Response = new FeedbackPaginatedDto
        {
            Size = 20,
            NextPageToken = "next-token-1",
            Content = new List<FeedbackDTO>
            {
                new FeedbackDTO
                {
                    Id = "fb-1",
                    Rate = "5",
                    Comment = "Excellent",
                    UserName = "Anna",
                    UserAvatarUrl = "http://img/u1",
                    Date = "2026-03-05T12:00:00Z",
                    Type = "GENERAL",
                    LocationId = "loc-1"
                }
            }
        };

        var res = await _client.GetAsync("/locations/loc-1/feedbacks?type=GENERAL");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.FeedbackService.LastLocationId.Should().Be("loc-1");
        _factory.FeedbackService.LastType.Should().Be("GENERAL");
        _factory.FeedbackService.LastSize.Should().Be(20);
        _factory.FeedbackService.LastSort.Should().BeEquivalentTo(new[] { "date,asc" });
        _factory.FeedbackService.LastPageToken.Should().BeNull();

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("size").GetInt32().Should().Be(20);
        data.GetPropertyIgnoreCase("nextPageToken").GetString().Should().Be("next-token-1");

        var content = data.GetPropertyIgnoreCase("content");
        content.GetArrayLength().Should().Be(1);
        content[0].GetPropertyIgnoreCase("id").GetString().Should().Be("fb-1");
        content[0].GetPropertyIgnoreCase("rate").GetString().Should().Be("5");
        content[0].GetPropertyIgnoreCase("comment").GetString().Should().Be("Excellent");
        content[0].GetPropertyIgnoreCase("userName").GetString().Should().Be("Anna");
        content[0].GetPropertyIgnoreCase("userAvatarUrl").GetString().Should().Be("http://img/u1");
        content[0].GetPropertyIgnoreCase("date").GetString().Should().Be("2026-03-05T12:00:00Z");
        content[0].GetPropertyIgnoreCase("type").GetString().Should().Be("GENERAL");
        content[0].GetPropertyIgnoreCase("locationId").GetString().Should().Be("loc-1");
    }

    [Fact]
    public async Task GetFeedbacks_WithExplicitParameters_ShouldPassAllQueryParamsToService()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.Response = new FeedbackPaginatedDto
        {
            Size = 5,
            NextPageToken = "next-token-2",
            Content = new List<FeedbackDTO>()
        };

        var res = await _client.GetAsync("/locations/loc-1/feedbacks?type=GENERAL&size=5&sort=rate,desc&sort=date,asc&pageToken=token-123");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.FeedbackService.LastLocationId.Should().Be("loc-1");
        _factory.FeedbackService.LastType.Should().Be("GENERAL");
        _factory.FeedbackService.LastSize.Should().Be(5);
        _factory.FeedbackService.LastSort.Should().Equal("rate,desc", "date,asc");
        _factory.FeedbackService.LastPageToken.Should().Be("token-123");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("size").GetInt32().Should().Be(5);
        data.GetPropertyIgnoreCase("nextPageToken").GetString().Should().Be("next-token-2");
    }

    [Fact]
    public async Task GetSpecialityDishes_ShouldReturn200_AndMapAllFields()
    {
        _factory.DishService.Reset();
        _factory.DishService.DishesByLocation["loc-1"] = new List<Dish>
        {
            new Dish
            {
                Id = "dish-1",
                Name = "Steak",
                ImageUrl = "http://img/dish-1",
                Price = 25.5f,
                State = "ON",
                Weight = 320
            }
        };

        var res = await _client.GetAsync("/locations/loc-1/speciality-dishes");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastLocationId.Should().Be("loc-1");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetArrayLength().Should().Be(1);
        data[0].GetPropertyIgnoreCase("id").GetString().Should().Be("dish-1");
        data[0].GetPropertyIgnoreCase("name").GetString().Should().Be("Steak");
        data[0].GetPropertyIgnoreCase("previewImageUrl").GetString().Should().Be("http://img/dish-1");
        data[0].GetPropertyIgnoreCase("price").GetDouble().Should().BeApproximately(25.5, 0.001);
        data[0].GetPropertyIgnoreCase("state").GetString().Should().Be("ON");
        data[0].GetPropertyIgnoreCase("weight").GetInt32().Should().Be(320);
    }

    [Fact]
    public async Task GetSpecialityDishes_WhenEmpty_ShouldReturn200_WithEmptyArray()
    {
        _factory.DishService.Reset();
        _factory.DishService.DishesByLocation["loc-empty"] = Array.Empty<Dish>();

        var res = await _client.GetAsync("/locations/loc-empty/speciality-dishes");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("data").GetArrayLength().Should().Be(0);
    }
}
