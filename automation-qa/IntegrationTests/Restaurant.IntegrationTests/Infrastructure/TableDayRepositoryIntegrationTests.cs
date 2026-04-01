using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.Infrastructure.IntegrationTests;

[Collection("DynamoDb collection")]
public sealed class TableDayRepositoryIntegrationTests
{
    private readonly DynamoDBContext _context;
    private readonly TableDayRepository _repo;

    public TableDayRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _repo = new TableDayRepository(_context);
    }

    [Fact]
    public async Task GetByTableAndDateAsync_ShouldReturnInsertedRecord()
    {
        var tableKey = $"loc-{Guid.NewGuid():N}#1";
        var date = "2026-06-01";

        var record = new TableDay
        {
            TableKey = tableKey,
            Date = date,
            ReservedSlots = new HashSet<string> { "10:00", "10:15" },
            Ttl = 9999999999L
        };

        await _context.SaveAsync(record);

        var result = await _repo.GetByTableAndDateAsync(tableKey, date, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TableKey.Should().Be(tableKey);
        result.Date.Should().Be(date);
        result.ReservedSlots.Should().BeEquivalentTo(new[] { "10:00", "10:15" });
    }

    [Fact]
    public async Task GetByTableAndDateAsync_WhenNotFound_ShouldReturnNull()
    {
        var result = await _repo.GetByTableAndDateAsync(
            $"loc-{Guid.NewGuid():N}#999", "2099-01-01", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTableAndDateAsync_WhenTableKeyIsEmpty_ShouldReturnNull()
    {
        var result = await _repo.GetByTableAndDateAsync("  ", "2026-06-01", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetManyByTablesAndDateAsync_ShouldReturnAllMatchingRecords()
    {
        var date = "2026-06-15";
        var key1 = $"loc-{Guid.NewGuid():N}#1";
        var key2 = $"loc-{Guid.NewGuid():N}#2";

        await _context.SaveAsync(new TableDay
        {
            TableKey = key1,
            Date = date,
            ReservedSlots = new HashSet<string> { "11:00" },
            Ttl = 9999999999L
        });

        await _context.SaveAsync(new TableDay
        {
            TableKey = key2,
            Date = date,
            ReservedSlots = new HashSet<string> { "12:00" },
            Ttl = 9999999999L
        });

        var result = await _repo.GetManyByTablesAndDateAsync(new[] { key1, key2 }, date, CancellationToken.None);

        result.Should().ContainKey(key1);
        result.Should().ContainKey(key2);
        result[key1].ReservedSlots.Should().Contain("11:00");
        result[key2].ReservedSlots.Should().Contain("12:00");
    }

    [Fact]
    public async Task GetManyByTablesAndDateAsync_WhenKeysMissing_ShouldReturnOnlyFoundRecords()
    {
        var date = "2026-07-01";
        var existingKey = $"loc-{Guid.NewGuid():N}#3";
        var missingKey = $"loc-{Guid.NewGuid():N}#999";

        await _context.SaveAsync(new TableDay
        {
            TableKey = existingKey,
            Date = date,
            ReservedSlots = new HashSet<string>(),
            Ttl = 9999999999L
        });

        var result = await _repo.GetManyByTablesAndDateAsync(new[] { existingKey, missingKey }, date, CancellationToken.None);

        result.Should().ContainKey(existingKey);
        result.Should().NotContainKey(missingKey);
    }

    [Fact]
    public async Task GetManyByTablesAndDateAsync_WhenEmptyInput_ShouldReturnEmptyDictionary()
    {
        var result = await _repo.GetManyByTablesAndDateAsync(
            Array.Empty<string>(), "2026-06-01", CancellationToken.None);

        result.Should().BeEmpty();
    }
}
