using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;
using Restaurant.IntegrationTests.Infrastructure;

namespace Restaurant.Infrastructure.IntegrationTests;

[Collection("DynamoDb collection")]
public sealed class ReservationRepositoryIntegrationTests
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
            CustomerName = "Customer One",
            WaiterId = "waiter-1",
            WaiterName = "Waiter One",
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
            CustomerName = "Customer X",
            WaiterId = "waiter-x",
            WaiterName = "Waiter X",
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
            CustomerName = "Customer Y",
            WaiterId = "waiter-y",
            WaiterName = "Waiter Y",
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
            CustomerName = "Create Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Create Waiter",
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
            CustomerName = "First Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "First Waiter",
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
            CustomerName = "Second Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Second Waiter",
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
            CustomerName = "Cancel Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Cancel Waiter",
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
            CustomerName = "Update Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Update Waiter",
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
            CustomerName = existing.CustomerName,
            WaiterId = existing.WaiterId,
            WaiterName = existing.WaiterName,
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

        result.IsSuccess.Should().BeTrue();

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
            CustomerName = "Move Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Move Waiter",
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
            CustomerName = existing.CustomerName,
            WaiterId = existing.WaiterId,
            WaiterName = existing.WaiterName,
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

        result.IsSuccess.Should().BeTrue();

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


    [Fact]
    public async Task UpdateLifecycleAsync_StartReservation_ShouldSetStatusInProgress_AndActualStartTime()
    {
        var id = Guid.NewGuid().ToString("N");
        var reservation = new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            CustomerName = "Lifecycle Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Lifecycle Waiter",
            LocationId = "loc-lifecycle",
            TableNumber = 2,
            TableKey = "loc-lifecycle#2",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(-15).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(reservation);

        reservation.Status = ReservationStatus.InProgress;
        reservation.ActualStartTime = DateTimeOffset.UtcNow.ToString("O");
        reservation.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        var result = await _repo.UpdateLifecycleAsync(
            reservation,
            ReservationStatus.Reserved,
            ReservationStatus.InProgress);

        result.Should().BeTrue();

        var loaded = await _repo.GetByIdAsync(id);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(ReservationStatus.InProgress);
        loaded.ActualStartTime.Should().NotBeNullOrWhiteSpace();
        loaded.ActualEndTime.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_MarkMealServed_ShouldSetFlag_WithoutChangingSlots()
    {
        var id = Guid.NewGuid().ToString("N");
        var tableKey = $"loc-lifecycle#{Guid.NewGuid():N}";
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var reservation = new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            CustomerName = "Meals Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Meals Waiter",
            LocationId = "loc-lifecycle",
            TableNumber = 3,
            TableKey = tableKey,
            StartDateTime = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(18, 0)), DateTimeKind.Utc)).ToString("O"),
            EndDateTime = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(19, 0)), DateTimeKind.Utc)).ToString("O"),
            ActualStartTime = DateTimeOffset.UtcNow.ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(reservation);
        await _context.SaveAsync(new TableDay
        {
            TableKey = tableKey,
            Date = date.ToString("yyyy-MM-dd"),
            ReservedSlots = new HashSet<string>(new[]
            {
                $"{date:yyyy-MM-dd}T18:00+00:00",
                $"{date:yyyy-MM-dd}T18:15+00:00",
                $"{date:yyyy-MM-dd}T18:30+00:00",
                $"{date:yyyy-MM-dd}T18:45+00:00",
                $"{date:yyyy-MM-dd}T19:00+00:00"
            }),
            Ttl = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds()
        });

        reservation.IsMealServed = true;
        reservation.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        await _repo.UpdateAsync(reservation);

        var loaded = await _repo.GetByIdAsync(id);
        loaded!.Status.Should().Be(ReservationStatus.InProgress);
        loaded.IsMealServed.Should().BeTrue();

        var tableDay = await _context.LoadAsync<TableDay>(tableKey, date.ToString("yyyy-MM-dd"));
        tableDay!.ReservedSlots.Should().HaveCount(5);
    }

    [Fact]
    public async Task UpdateLifecycleAsync_FinishReservation_ShouldSetFinished_AndReleaseSlots()
    {
        var id = Guid.NewGuid().ToString("N");
        var tableKey = $"loc-finish#{Guid.NewGuid():N}";
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var slots = new List<string>
        {
            $"{date:yyyy-MM-dd}T18:00+00:00",
            $"{date:yyyy-MM-dd}T18:15+00:00",
            $"{date:yyyy-MM-dd}T18:30+00:00",
            $"{date:yyyy-MM-dd}T18:45+00:00",
            $"{date:yyyy-MM-dd}T19:00+00:00"
        };

        var reservation = new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            CustomerName = "Finish Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Finish Waiter",
            LocationId = "loc-finish",
            TableNumber = 5,
            TableKey = tableKey,
            StartDateTime = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(18, 0)), DateTimeKind.Utc)).ToString("O"),
            EndDateTime = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(19, 0)), DateTimeKind.Utc)).ToString("O"),
            ActualStartTime = DateTimeOffset.UtcNow.AddMinutes(-45).ToString("O"),
            GuestsCount = 4,
            Status = ReservationStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(reservation);
        await _context.SaveAsync(new TableDay
        {
            TableKey = tableKey,
            Date = date.ToString("yyyy-MM-dd"),
            ReservedSlots = new HashSet<string>(slots.Concat(new[] { $"{date:yyyy-MM-dd}T20:00+00:00" })),
            Ttl = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds()
        });

        reservation.Status = ReservationStatus.Finished;
        reservation.ActualEndTime = DateTimeOffset.UtcNow.ToString("O");
        reservation.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        var result = await _repo.UpdateLifecycleAsync(
            reservation,
            ReservationStatus.InProgress,
            ReservationStatus.Finished,
            slots);

        result.Should().BeTrue();

        var loaded = await _repo.GetByIdAsync(id);
        loaded!.Status.Should().Be(ReservationStatus.Finished);
        loaded.ActualEndTime.Should().NotBeNullOrWhiteSpace();

        var tableDay = await _context.LoadAsync<TableDay>(tableKey, date.ToString("yyyy-MM-dd"));
        tableDay!.ReservedSlots.Should().BeEquivalentTo(new[] { $"{date:yyyy-MM-dd}T20:00+00:00" });
    }



    [Fact]
    public async Task UpdateLifecycleAsync_WhenExpectedStatusDoesNotMatch_ShouldReturnFalse()
    {
        var id = Guid.NewGuid().ToString("N");
        var reservation = new Reservation
        {
            Id = id,
            CustomerId = $"customer-{Guid.NewGuid():N}",
            CustomerName = "Mismatch Customer",
            WaiterId = $"waiter-{Guid.NewGuid():N}",
            WaiterName = "Mismatch Waiter",
            LocationId = "loc-status-mismatch",
            TableNumber = 1,
            TableKey = "loc-status-mismatch#1",
            StartDateTime = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("O"),
            EndDateTime = DateTimeOffset.UtcNow.AddMinutes(30).ToString("O"),
            ActualStartTime = DateTimeOffset.UtcNow.AddMinutes(-25).ToString("O"),
            GuestsCount = 2,
            Status = ReservationStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        await _context.SaveAsync(reservation);

        reservation.Status = ReservationStatus.Finished;
        reservation.ActualEndTime = DateTimeOffset.UtcNow.ToString("O");
        reservation.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");

        var result = await _repo.UpdateLifecycleAsync(
            reservation,
            ReservationStatus.Reserved,
            ReservationStatus.Finished,
            new List<string>());

        result.Should().BeFalse();

        var loaded = await _repo.GetByIdAsync(id);
        loaded!.Status.Should().Be(ReservationStatus.InProgress);
        loaded.ActualEndTime.Should().BeNull();
    }

}