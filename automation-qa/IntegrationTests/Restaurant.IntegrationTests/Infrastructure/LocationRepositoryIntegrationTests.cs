using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.Infrastructure.IntegrationTests;

public sealed class LocationRepositoryIntegrationTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDBContext _context;
    private readonly LocationRepository _repo;

    public LocationRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _repo = new LocationRepository(_context);
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
            Rating = 4.2
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
            Rating = 4.8
        });

        var items = await _repo.GetLocationOptionsAsync();

        items.Should().Contain(x => x.Id == id && x.Address == "Hamburg, Test str 2");
    }
}