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

    [Fact]
    public async Task CreateWithSlotsAsync_ShouldPersistReservation_AndReserveSlots()
    {
        var id = Guid.NewGuid().ToString("N");
        var tableKey = $"loc-create#{Guid.NewGuid():N}";
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var start = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(18, 0)), DateTimeKind.Utc));
        var end = start.AddMinutes(90);
        var slots = new List<string> { "18:00", "18:30", "19:00" };

        var reservation = new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            LocationId = "loc-create",
            TableNumber = 7,
            TableKey = tableKey,
            StartDateTime = start.ToString("O"),
            EndDateTime = end.ToString("O"),
            GuestsCount = 4,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var created = await _repo.CreateWithSlotsAsync(reservation, date, slots);

        created.Should().BeTrue();

        var loaded = await _repo.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.TableKey.Should().Be(tableKey);

        var tableDay = await _context.LoadAsync<TableDay>(tableKey, date.ToString("yyyy-MM-dd"));
        tableDay.Should().NotBeNull();
        tableDay!.ReservedSlots.Should().BeEquivalentTo(slots);
    }

    [Fact]
    public async Task CreateWithSlotsAsync_WhenSlotsOverlap_ShouldReturnFalse_AndNotPersistSecondReservation()
    {
        var tableKey = $"loc-overlap#{Guid.NewGuid():N}";
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6));

        var firstId = Guid.NewGuid().ToString("N");
        var secondId = Guid.NewGuid().ToString("N");
        var firstSlots = new List<string> { "19:00", "19:30" };
        var overlappingSlots = new List<string> { "19:30", "20:00" };

        var first = new Reservation
        {
            Id = firstId,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            LocationId = "loc-overlap",
            TableNumber = 5,
            TableKey = tableKey,
            StartDateTime = DateTimeOffset.UtcNow.AddDays(6).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddDays(6).AddMinutes(90).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var second = new Reservation
        {
            Id = secondId,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            LocationId = "loc-overlap",
            TableNumber = 5,
            TableKey = tableKey,
            StartDateTime = DateTimeOffset.UtcNow.AddDays(6).AddHours(1).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddDays(6).AddHours(2).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var firstCreated = await _repo.CreateWithSlotsAsync(first, date, firstSlots);
        var secondCreated = await _repo.CreateWithSlotsAsync(second, date, overlappingSlots);

        firstCreated.Should().BeTrue();
        secondCreated.Should().BeFalse();

        var secondLoaded = await _repo.GetByIdAsync(secondId);
        secondLoaded.Should().BeNull();

        var tableDay = await _context.LoadAsync<TableDay>(tableKey, date.ToString("yyyy-MM-dd"));
        tableDay.Should().NotBeNull();
        tableDay!.ReservedSlots.Should().BeEquivalentTo(firstSlots);
    }

    [Fact]
    public async Task CancelReservationAsync_ShouldSetStatusCancelled_AndRemoveGivenSlotsFromTableDay()
    {
        var id = Guid.NewGuid().ToString("N");
        var tableKey = $"loc-cancel#{Guid.NewGuid():N}";
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var reservation = new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            LocationId = "loc-cancel",
            TableNumber = 9,
            TableKey = tableKey,
            StartDateTime = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(18, 0)), DateTimeKind.Utc)).ToString("O"),
            EndDateTime = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(19, 30)), DateTimeKind.Utc)).ToString("O"),
            GuestsCount = 4,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var slotsToRemove = new List<string> { "18:00", "18:30", "19:00" };

        await _context.SaveAsync(reservation);
        await _context.SaveAsync(new TableDay
        {
            TableKey = tableKey,
            Date = date.ToString("yyyy-MM-dd"),
            ReservedSlots = new HashSet<string>(new[] { "18:00", "18:30", "19:00", "20:00" }),
            Ttl = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds()
        });

        var cancelled = await _repo.CancelReservationAsync(reservation, slotsToRemove);

        cancelled.Should().BeTrue();

        var updatedReservation = await _repo.GetByIdAsync(id);
        updatedReservation.Should().NotBeNull();
        updatedReservation!.Status.Should().Be(ReservationStatus.Cancelled);

        var tableDay = await _context.LoadAsync<TableDay>(tableKey, date.ToString("yyyy-MM-dd"));
        tableDay.Should().NotBeNull();
        tableDay!.ReservedSlots.Should().BeEquivalentTo(new[] { "20:00" });
    }

    [Fact]
    public async Task UpdateReservationAsync_SameTableSameDay_ShouldUpdateReservation_AndAdjustSlots()
    {
        var id = Guid.NewGuid().ToString("N");
        var tableKey = $"loc-update#{Guid.NewGuid():N}";
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8));
        var oldStart = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(18, 0)), DateTimeKind.Utc));

        var existing = new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            LocationId = "loc-update",
            TableNumber = 4,
            TableKey = tableKey,
            StartDateTime = oldStart.ToString("O"),
            EndDateTime = oldStart.AddMinutes(90).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(existing);
        await _context.SaveAsync(new TableDay
        {
            TableKey = tableKey,
            Date = date.ToString("yyyy-MM-dd"),
            ReservedSlots = new HashSet<string>(new[] { "18:00", "18:30", "19:00" }),
            Ttl = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds()
        });

        var updated = new Reservation
        {
            Id = id,
            CustomerId = existing.CustomerId,
            WaiterId = existing.WaiterId,
            LocationId = existing.LocationId,
            TableNumber = existing.TableNumber,
            TableKey = tableKey,
            StartDateTime = oldStart.AddMinutes(30).ToString("O"),
            EndDateTime = oldStart.AddHours(2).ToString("O"),
            GuestsCount = 3,
            Status = ReservationStatus.Reserved,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var oldSlots = new List<string> { "18:00", "18:30", "19:00" };
        var newSlots = new List<string> { "18:30", "19:00", "19:30" };

        var result = await _repo.UpdateReservationAsync(updated, newSlots, oldSlots, tableKey, oldStart);

        result.Should().NotBeNull();

        var loaded = await _repo.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.GuestsCount.Should().Be(3);
        loaded.StartDateTime.Should().Be(updated.StartDateTime);

        var tableDay = await _context.LoadAsync<TableDay>(tableKey, date.ToString("yyyy-MM-dd"));
        tableDay.Should().NotBeNull();
        tableDay!.ReservedSlots.Should().BeEquivalentTo(new[] { "18:30", "19:00", "19:30" });
    }

    [Fact]
    public async Task UpdateReservationAsync_DifferentTableOrDay_ShouldMoveSlotsBetweenTableDays()
    {
        var id = Guid.NewGuid().ToString("N");
        var oldTableKey = $"loc-old#{Guid.NewGuid():N}";
        var newTableKey = $"loc-new#{Guid.NewGuid():N}";
        var oldDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9));
        var newDate = oldDate.AddDays(1);
        var oldStart = new DateTimeOffset(DateTime.SpecifyKind(oldDate.ToDateTime(new TimeOnly(18, 0)), DateTimeKind.Utc));

        var existing = new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            LocationId = "loc-move",
            TableNumber = 2,
            TableKey = oldTableKey,
            StartDateTime = oldStart.ToString("O"),
            EndDateTime = oldStart.AddMinutes(60).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(existing);
        await _context.SaveAsync(new TableDay
        {
            TableKey = oldTableKey,
            Date = oldDate.ToString("yyyy-MM-dd"),
            ReservedSlots = new HashSet<string>(new[] { "18:00", "18:30", "19:00" }),
            Ttl = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds()
        });

        var updated = new Reservation
        {
            Id = id,
            CustomerId = existing.CustomerId,
            WaiterId = existing.WaiterId,
            LocationId = existing.LocationId,
            TableNumber = 6,
            TableKey = newTableKey,
            StartDateTime = new DateTimeOffset(DateTime.SpecifyKind(newDate.ToDateTime(new TimeOnly(20, 0)), DateTimeKind.Utc)).ToString("O"),
            EndDateTime = new DateTimeOffset(DateTime.SpecifyKind(newDate.ToDateTime(new TimeOnly(21, 0)), DateTimeKind.Utc)).ToString("O"),
            GuestsCount = 4,
            Status = ReservationStatus.Reserved,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var oldSlots = new List<string> { "18:00", "18:30" };
        var newSlots = new List<string> { "20:00", "20:30" };

        var result = await _repo.UpdateReservationAsync(updated, newSlots, oldSlots, oldTableKey, oldStart);

        result.Should().NotBeNull();

        var loaded = await _repo.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.TableKey.Should().Be(newTableKey);
        loaded.TableNumber.Should().Be(6);

        var oldTableDay = await _context.LoadAsync<TableDay>(oldTableKey, oldDate.ToString("yyyy-MM-dd"));
        oldTableDay.Should().NotBeNull();
        oldTableDay!.ReservedSlots.Should().BeEquivalentTo(new[] { "19:00" });

        var newTableDay = await _context.LoadAsync<TableDay>(newTableKey, newDate.ToString("yyyy-MM-dd"));
        newTableDay.Should().NotBeNull();
        newTableDay!.ReservedSlots.Should().BeEquivalentTo(newSlots);
    }
}