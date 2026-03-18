using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.Infrastructure.IntegrationTests;

[Collection("DynamoDb collection")]
public sealed class ReservationRepositoryIntegrationTests
    : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDBContext _context;
    private readonly IAmazonDynamoDB _client;
    private readonly ReservationRepository _repo;

    public ReservationRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _client = fixture.Client;
        _repo = new ReservationRepository(_context, _client);
    }

    [Fact]
    public async Task QueryByCustomerAsync_ShouldReturnInsertedReservation()
    {
        var id = Guid.NewGuid().ToString("N");

        var item = new Reservation
        {
            Id = id,
            CustomerId = "customer-1",
            WaiterId = "waiter-1",
            LocationId = "loc-1",
            TableNumber = 3,
            TableKey = "loc-1#3",
            StartDateTime = DateTimeOffset.UtcNow.AddDays(1).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1).AddMinutes(90).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(item);

        var found = await _repo.QueryByCustomerAsync("customer-1");

        found.Should().ContainSingle(x => x.Id == id);
    }

    [Fact]
    public async Task QueryByWaiterAsync_ShouldReturnInsertedReservation()
    {
        var id = Guid.NewGuid().ToString("N");

        var item = new Reservation
        {
            Id = id,
            CustomerId = "customer-x",
            WaiterId = "waiter-x",
            LocationId = "loc-1",
            TableNumber = 1,
            TableKey = "loc-1#1",
            StartDateTime = DateTimeOffset.UtcNow.AddDays(2).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddDays(2).AddMinutes(90).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(item);

        var found = await _repo.QueryByWaiterAsync("waiter-x");

        found.Should().Contain(x => x.Id == id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnInsertedReservation()
    {
        var id = Guid.NewGuid().ToString("N");

        var item = new Reservation
        {
            Id = id,
            CustomerId = "customer-y",
            WaiterId = "waiter-y",
            LocationId = "loc-1",
            TableNumber = 2,
            TableKey = "loc-1#2",
            StartDateTime = DateTimeOffset.UtcNow.AddDays(3).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddDays(3).AddMinutes(90).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(item);

        var loaded = await _repo.GetByIdAsync(id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
    }
}