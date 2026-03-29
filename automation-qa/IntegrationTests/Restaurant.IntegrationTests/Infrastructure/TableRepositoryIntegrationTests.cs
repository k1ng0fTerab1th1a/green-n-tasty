using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;

namespace Restaurant.IntegrationTests.Infrastructure;

public sealed class TableRepositoryIntegrationTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDBContext _context;
    private readonly TableRepository _repo;

    public TableRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _repo = new TableRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnInsertedTable()
    {
        var locationId = $"loc-{Guid.NewGuid():N}";

        var table = new Table
        {
            LocationId = locationId,
            TableNumber = 1,
            LocationAddress = "All St 1",
            Capacity = 4
        };

        await _context.SaveAsync(table);

        var result = await _repo.GetAllAsync(CancellationToken.None);

        result.Should().Contain(t => t.LocationId == locationId && t.TableNumber == 1);
    }

    [Fact]
    public async Task GetByLocationIdAsync_ShouldReturnOnlyTablesForThatLocation()
    {
        var locationId = $"loc-{Guid.NewGuid():N}";

        var t1 = new Table { LocationId = locationId, TableNumber = 1, LocationAddress = "Addr 1", Capacity = 2 };
        var t2 = new Table { LocationId = locationId, TableNumber = 2, LocationAddress = "Addr 1", Capacity = 4 };
        var other = new Table { LocationId = $"loc-{Guid.NewGuid():N}", TableNumber = 1, LocationAddress = "Other", Capacity = 6 };

        await _context.SaveAsync(t1);
        await _context.SaveAsync(t2);
        await _context.SaveAsync(other);

        var result = await _repo.GetByLocationIdAsync(locationId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.LocationId.Should().Be(locationId));
        result.Select(t => t.TableNumber).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task GetByLocationAndTableNumberAsync_ShouldReturnCorrectTable()
    {
        var locationId = $"loc-{Guid.NewGuid():N}";

        var table = new Table
        {
            LocationId = locationId,
            TableNumber = 5,
            LocationAddress = "Specific St 5",
            Capacity = 8
        };

        await _context.SaveAsync(table);

        var found = await _repo.GetByLocationAndTableNumberAsync(locationId, 5, CancellationToken.None);

        found.Should().NotBeNull();
        found!.LocationId.Should().Be(locationId);
        found.TableNumber.Should().Be(5);
        found.LocationAddress.Should().Be("Specific St 5");
        found.Capacity.Should().Be(8);
    }

    [Fact]
    public async Task GetByLocationAndTableNumberAsync_WhenNotFound_ShouldReturnNull()
    {
        var result = await _repo.GetByLocationAndTableNumberAsync(
            $"loc-{Guid.NewGuid():N}", 99, CancellationToken.None);

        result.Should().BeNull();
    }
}
