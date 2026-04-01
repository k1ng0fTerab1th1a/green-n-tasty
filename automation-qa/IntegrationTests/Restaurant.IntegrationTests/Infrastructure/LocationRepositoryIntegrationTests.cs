using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.Infrastructure.IntegrationTests;

[Collection("DynamoDb collection")]
public sealed class LocationRepositoryIntegrationTests 
{
    private readonly DynamoDBContext _context;
    private readonly IAmazonDynamoDB _client;
    private readonly LocationRepository _repo;

    public LocationRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _client = fixture.Client;
        _repo = new LocationRepository(_context, _client);
    }

    [Fact]
    public async Task GetLocationsAsync_ShouldReturnInsertedLocation()
    {
        var id = Guid.NewGuid().ToString("N");

        await _context.SaveAsync(new Location
        {
            Id = id,
            Address = "Berlin, Test str 1",
            Description = "Test",
            TotalCapacity = 10,
            AverageOccupancy = 0.5,
            ImageUrl = "http://img",
            TotalRating = 420,
            FeedbacksAmount = 100 
        });

        var items = await _repo.GetLocationsAsync();

        items.Should().Contain(x => x.Id == id);
    }

    [Fact]
    public async Task GetLocationOptionsAsync_ShouldReturnInsertedLocation()
    {
        var id = Guid.NewGuid().ToString("N");

        await _context.SaveAsync(new Location
        {
            Id = id,
            Address = "Hamburg, Test str 2",
            Description = "Test",
            TotalCapacity = 20,
            AverageOccupancy = 0.7,
            ImageUrl = "http://img2",
            TotalRating = 480,
            FeedbacksAmount = 100 
        });

        var items = await _repo.GetLocationOptionsAsync();

        items.Should().Contain(x => x.Id == id && x.Address == "Hamburg, Test str 2");
    }

    [Fact]
    public async Task GetByIdAsync_WhenLocationExists_ShouldReturnLocation()
    {
        var id = Guid.NewGuid().ToString("N");

        await _context.SaveAsync(new Location
        {
            Id = id,
            Address = "Munich, Test str 3",
            Description = "Test by id",
            TotalCapacity = 40,
            AverageOccupancy = 0.2,
            ImageUrl = "http://img3",
            TotalRating = 410,
            FeedbacksAmount = 100 
        });

        var item = await _repo.GetByIdAsync(id);

        item.Should().NotBeNull();
        item!.Id.Should().Be(id);
        item.Address.Should().Be("Munich, Test str 3");
    }

    [Fact]
    public async Task GetByIdAsync_WhenLocationDoesNotExist_ShouldReturnNull()
    {
        var item = await _repo.GetByIdAsync(Guid.NewGuid().ToString("N"));

        item.Should().BeNull();
    }
}