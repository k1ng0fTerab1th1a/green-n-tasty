using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.UnitTests.Services;

public sealed class TableServiceTests
{
    private static readonly DateOnly FutureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
    private static readonly string FutureDateStr = FutureDate.ToString("yyyy-MM-dd");
    private static readonly string FutureDateNextDayStr = FutureDate.AddDays(1).ToString("yyyy-MM-dd");

    private static DateTimeOffset FutureDateAt(int hour, int minute, int utcOffsetHours = 0) =>
        new(FutureDate.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.FromHours(utcOffsetHours));

    private static DateTimeOffset FutureDateNextDayAt(int hour, int minute, int utcOffsetHours = 0) =>
        new(FutureDate.AddDays(1).ToDateTime(new TimeOnly(hour, minute)), TimeSpan.FromHours(utcOffsetHours));


    private readonly Mock<ITableRepository> _tableRepo;
    private readonly Mock<ITableDayRepository> _tableDayRepo;
    private readonly Mock<ILocationRepository> _locationRepo;
    private readonly Mock<IReservationRepository> _reservationRepo;
    private readonly TableService _sut;

    public TableServiceTests()
    {
        _tableRepo = new Mock<ITableRepository>(MockBehavior.Strict);
        _tableDayRepo = new Mock<ITableDayRepository>(MockBehavior.Strict);
        _locationRepo = new Mock<ILocationRepository>(MockBehavior.Strict);
        _reservationRepo = new Mock<IReservationRepository>(MockBehavior.Strict);
        _sut = new TableService(_tableRepo.Object, _tableDayRepo.Object, _locationRepo.Object, _reservationRepo.Object);
    }

    private static Location MakeUtcLocation(string id, string openTime = "10:00", string closeTime = "22:00") =>
        new()
        {
            Id = id,
            Address = $"Address for {id}",
            TimeZone = "UTC",
            OpenTime = openTime,
            CloseTime = closeTime,
            Description = "Test",
            AverageOccupancy = 0.5,
            ImageUrl = "http://img",
            TotalCapacity = 100,
            TotalRating = 400,
            FeedbacksAmount = 100 
        };

    private static Table MakeTable(string locationId, int tableNumber, int capacity = 4) =>
        new()
        {
            LocationId = locationId,
            TableNumber = tableNumber,
            LocationAddress = $"Address for {locationId}",
            Capacity = capacity
        };

    private static Reservation MakeReservation(string id, string tableKey, DateTimeOffset start, DateTimeOffset end)
    {
        var parts = tableKey.Split('#', 2);
        return new()
        {
            Id = id,
            TableKey = tableKey,
            TableNumber = int.Parse(parts[1]),
            LocationId = parts[0],
            LocationAddress = $"Address for {parts[0]}",
            WaiterId = "waiter-1",
            WaiterName = "Waiter",
            StartDateTime = start.ToString("O"),
            EndDateTime = end.ToString("O"),
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O"),
        };
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenLocationIdIsNull_ShouldCallGetAllAsync()
    {
        _tableRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table>().AsReadOnly());

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, null, null, null), ct: default);

        result.Value.Should().BeEmpty();
        _tableRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tableRepo.VerifyNoOtherCalls();
        _tableDayRepo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenLocationIdIsProvided_ShouldCallGetByLocationIdAsync()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table>().AsReadOnly());

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, null), ct: default);

        result.Value.Should().BeEmpty();
        _tableRepo.Verify(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()), Times.Once);
        _tableRepo.VerifyNoOtherCalls();
        _tableDayRepo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenCapacityFilterExcludesAllTables_ShouldReturnEmptyWithoutQueryingFurtherRepos()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1, capacity: 2) }.AsReadOnly());

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", 4, null), ct: default);

        result.Value.Should().BeEmpty();
        _tableRepo.Verify(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()), Times.Once);
        _tableRepo.VerifyNoOtherCalls();
        _tableDayRepo.VerifyNoOtherCalls();
        _locationRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenLocationNotFound_ShouldThrowInvalidDataException()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        var act = async () => await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, null), ct: default);

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenNoReservations_ShouldReturnTableWithOneContiguousSlotSpanningFullShift()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.Is<IEnumerable<string>>(keys => keys.Contains("loc1#1")),
                FutureDateStr,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, null), ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].TableNumber.Should().Be(1);
        result.Value[0].AvailableSlots.Should().HaveCount(1);
        result.Value[0].AvailableSlots[0].StartOffset.Should().Be(FutureDateAt(10, 0));
        result.Value[0].AvailableSlots[0].EndOffset.Should().Be(FutureDateAt(21, 45));
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenRequestedTimeSlotIsReserved_ShouldExcludeTable()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string> { $"{FutureDateStr}T10:00+00:00" }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, new TimeOnly(10, 0), "loc1", null, null), ct: default);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenRequestedTimeSlotIsFree_ShouldIncludeTable()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, new TimeOnly(10, 0), "loc1", null, null), ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].TableNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenShiftHasShortFreeWindow_ShouldExcludeShortWindowFromSlots()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        // Shift 10:00�13:00. Reserving 11:15�12:00 (+15 min gap) creates:
        //   - free window 10:00�11:00 = 60 min -> qualifies, kept in slots
        //   - free window 12:15�12:45 = 30 min -> below threshold, excluded from slots
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "10:00", closeTime: "13:00"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string>
                    {
                        $"{FutureDateStr}T11:15+00:00",
                        $"{FutureDateStr}T11:30+00:00",
                        $"{FutureDateStr}T11:45+00:00",
                        $"{FutureDateStr}T12:00+00:00",
                    }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, null), ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].AvailableSlots.Should().HaveCount(1, because: "only the 60-min window qualifies; the 30-min window is below the threshold");
        result.Value[0].AvailableSlots[0].StartOffset.Should().Be(FutureDateAt(10, 0));
        result.Value[0].AvailableSlots[0].EndOffset.Should().Be(FutureDateAt(11, 0));
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenAllFreeWindowsAreShorterThan60Minutes_ShouldExcludeTableFromResults()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "10:00", closeTime: "10:45"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, null), ct: default);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenCapacityFilterApplied_ShouldOnlyReturnQualifyingTables()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table>
            {
                MakeTable("loc1", 1, capacity: 2),
                MakeTable("loc1", 2, capacity: 6)
            }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.Is<IEnumerable<string>>(keys => keys.Contains("loc1#2") && !keys.Contains("loc1#1")),
                FutureDateStr,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", 4, null), ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].TableNumber.Should().Be(2);
        result.Value[0].Capacity.Should().Be(6);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenTwoLocationsHaveDifferentTimeZones_ShouldTreatRequestedTimeAsLocalForEachLocation()
    {
        _tableRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table>
            {
                MakeTable("loc-utc", 1),
                MakeTable("loc-tbilisi", 1)
            }.AsReadOnly());

        _locationRepo.Setup(r => r.GetByIdAsync("loc-utc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc-utc"));
        _locationRepo.Setup(r => r.GetByIdAsync("loc-tbilisi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Location
            {
                Id = "loc-tbilisi",
                Address = "Address for loc-tbilisi",
                TimeZone = "Asia/Tbilisi",
                OpenTime = "10:00",
                CloseTime = "22:00",
                Description = "Test",
                AverageOccupancy = 0.5,
                ImageUrl = "http://img",
                TotalCapacity = 100,
                TotalRating = 400,
                FeedbacksAmount = 100 
            });

        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.Is<IEnumerable<string>>(keys => keys.Contains("loc-utc#1")),
                FutureDateStr,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.Is<IEnumerable<string>>(keys => keys.Contains("loc-tbilisi#1")),
                FutureDateStr,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc-tbilisi#1"] = new TableDay
                {
                    TableKey = "loc-tbilisi#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string> { $"{FutureDateStr}T13:00+04:00" }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, new TimeOnly(13, 0), null, null, null), ct: default);

        result.Value.Should().HaveCount(1, because: "the Tbilisi table has 13:00 local reserved; the UTC table does not");
        result.Value[0].LocationId.Should().Be("loc-utc");
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenShiftCrossesMidnight_ShouldReturnSingleContiguousSlotSpanningMidnight()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "14:00", closeTime: "02:00"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                FutureDateStr,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, null), ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].AvailableSlots.Should().HaveCount(1, because: "the entire 12-hour shift is free � no split at midnight");
        result.Value[0].AvailableSlots[0].StartOffset.Should().Be(FutureDateAt(14, 0));
        result.Value[0].AvailableSlots[0].EndOffset.Should().Be(FutureDateNextDayAt(1, 45));
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenShiftCrossesMidnightAndRequestedTimeIsAfterMidnight_ShouldMapTimeToNextDay()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "14:00", closeTime: "02:00"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                FutureDateStr,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string> { $"{FutureDateNextDayStr}T01:00+00:00" }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, new TimeOnly(1, 0), "loc1", null, null), ct: default);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenDateIsInThePast_ShouldReturnFailedResult()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(pastDate, null, null, null, null), ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(TableErrors.RequestedSlotsFromPast);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenDateIsTooFarInFuture_ShouldReturnFailedResult()
    {
        var farFutureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15);

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(farFutureDate, null, null, null, null), ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(TableErrors.RequestedSlotsFromFarFuture);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenDateIsTodayAndTimeIsInPastForLocationTimezone_ShouldExcludeTableFromResults()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastLocalTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddMinutes(-30));

        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "00:00", closeTime: "23:45"));

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(today, pastLocalTime, "loc1", null, null), ct: default);

        result.Value.Should().BeEmpty(because: "the requested time is in the past for this location's timezone");
        _tableDayRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenExcludeReservationIdIsNull_ShouldNotQueryReservationRepository()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1"));
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, null), ct: default);

        _reservationRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenExcludeReservationIdIsNotFound_ShouldNotFreeAnySlots()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "10:00", closeTime: "13:00"));
        _reservationRepo.Setup(r => r.GetByIdAsync("unknown-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string>
                    {
                        $"{FutureDateStr}T10:00+00:00",
                        $"{FutureDateStr}T10:15+00:00",
                        $"{FutureDateStr}T10:30+00:00",
                        $"{FutureDateStr}T10:45+00:00",
                        $"{FutureDateStr}T11:00+00:00",
                    }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, "unknown-id"), ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].AvailableSlots[0].StartOffset.Should().Be(FutureDateAt(11, 15),
            because: "the reserved slots are not freed since no matching reservation was found");
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenExcludeReservationIdIsValid_ShouldTreatItsReservedSlotsAsFree()
    {
        var reservation = MakeReservation("res-1", "loc1#1", FutureDateAt(10, 0), FutureDateAt(11, 0));

        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "10:00", closeTime: "13:00"));
        _reservationRepo.Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string>
                    {
                        $"{FutureDateStr}T10:00+00:00",
                        $"{FutureDateStr}T10:15+00:00",
                        $"{FutureDateStr}T10:30+00:00",
                        $"{FutureDateStr}T10:45+00:00",
                        $"{FutureDateStr}T11:00+00:00",
                    }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, "res-1"), ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].AvailableSlots.Should().HaveCount(1);
        result.Value[0].AvailableSlots[0].StartOffset.Should().Be(FutureDateAt(10, 0),
            because: "the excluded reservation's slots are freed, expanding the available window to start at shift open time");
        result.Value[0].AvailableSlots[0].EndOffset.Should().Be(FutureDateAt(12, 45));
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenExcludeReservationIdIsValid_OnlyItsSlotsShouldBeFreed()
    {
        // Shift 10:00–14:00. Excluded reservation holds 10:00–11:00. A separate 12:00 slot is reserved.
        // After exclusion: 10:00–11:00 freed → window [10:00–11:45] opens (105 min); [12:15–13:45] remains (90 min).
        var reservation = MakeReservation("res-1", "loc1#1", FutureDateAt(10, 0), FutureDateAt(11, 0));

        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "10:00", closeTime: "14:00"));
        _reservationRepo.Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string>
                    {
                        // Excluded reservation's slots (10:00–11:00)
                        $"{FutureDateStr}T10:00+00:00",
                        $"{FutureDateStr}T10:15+00:00",
                        $"{FutureDateStr}T10:30+00:00",
                        $"{FutureDateStr}T10:45+00:00",
                        $"{FutureDateStr}T11:00+00:00",
                        // A different reservation's slot at 12:00
                        $"{FutureDateStr}T12:00+00:00",
                    }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, "res-1"), ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].AvailableSlots.Should().HaveCount(2,
            because: "freeing the excluded reservation opens a new earlier window; the 12:00 slot from another reservation stays reserved");
        result.Value[0].AvailableSlots[0].StartOffset.Should().Be(FutureDateAt(10, 0));
        result.Value[0].AvailableSlots[0].EndOffset.Should().Be(FutureDateAt(11, 45));
        result.Value[0].AvailableSlots[1].StartOffset.Should().Be(FutureDateAt(12, 15));
        result.Value[0].AvailableSlots[1].EndOffset.Should().Be(FutureDateAt(13, 45));
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenRequestedTimeIsReservedByExcludedReservation_ShouldIncludeTable()
    {
        var reservation = MakeReservation("res-1", "loc1#1", FutureDateAt(10, 0), FutureDateAt(11, 0));

        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "10:00", closeTime: "14:00"));
        _reservationRepo.Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string>
                    {
                        $"{FutureDateStr}T10:00+00:00",
                        $"{FutureDateStr}T10:15+00:00",
                        $"{FutureDateStr}T10:30+00:00",
                        $"{FutureDateStr}T10:45+00:00",
                        $"{FutureDateStr}T11:00+00:00",
                    }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, new TimeOnly(10, 0), "loc1", null, "res-1"), ct: default);

        result.Value.Should().HaveCount(1,
            because: "the requested slot 10:00 belongs to the excluded reservation and is treated as free");
        result.Value[0].TableNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenRequestedTimeIsReservedByOtherReservation_ExclusionShouldNotFreeIt()
    {
        // Excluded reservation holds 11:00–12:00; requested time 10:00 is reserved by a different reservation.
        var reservation = MakeReservation("res-1", "loc1#1", FutureDateAt(11, 0), FutureDateAt(12, 0));

        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "10:00", closeTime: "14:00"));
        _reservationRepo.Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = FutureDateStr,
                    ReservedSlots = new HashSet<string>
                    {
                        // Different reservation holds the requested time
                        $"{FutureDateStr}T10:00+00:00",
                        // Excluded reservation's slots (11:00–12:00)
                        $"{FutureDateStr}T11:00+00:00",
                        $"{FutureDateStr}T11:15+00:00",
                        $"{FutureDateStr}T11:30+00:00",
                        $"{FutureDateStr}T11:45+00:00",
                        $"{FutureDateStr}T12:00+00:00",
                    }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, new TimeOnly(10, 0), "loc1", null, "res-1"), ct: default);

        result.Value.Should().BeEmpty(
            because: "the requested slot 10:00 is held by a different reservation, not the excluded one");
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenExcludedReservationIsForDifferentTable_ShouldNotFreeOtherTablesSlots()
    {
        // Excluded reservation is on table 1; table 2 has identical reserved slots that should remain reserved.
        var reservation = MakeReservation("res-1", "loc1#1", FutureDateAt(10, 0), FutureDateAt(11, 0));

        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1), MakeTable("loc1", 2) }.AsReadOnly());
        _locationRepo.Setup(r => r.GetByIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeUtcLocation("loc1", openTime: "10:00", closeTime: "13:00"));
        _reservationRepo.Setup(r => r.GetByIdAsync("res-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var reservedSlots = new HashSet<string>
        {
            $"{FutureDateStr}T10:00+00:00",
            $"{FutureDateStr}T10:15+00:00",
            $"{FutureDateStr}T10:30+00:00",
            $"{FutureDateStr}T10:45+00:00",
            $"{FutureDateStr}T11:00+00:00",
        };
        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay { TableKey = "loc1#1", Date = FutureDateStr, ReservedSlots = new HashSet<string>(reservedSlots) },
                ["loc1#2"] = new TableDay { TableKey = "loc1#2", Date = FutureDateStr, ReservedSlots = new HashSet<string>(reservedSlots) },
            });

        var result = await _sut.GetAvailableTablesAsync(new GetAvailableTablesQuery(FutureDate, null, "loc1", null, "res-1"), ct: default);

        result.Value.Should().HaveCount(2);
        var table1 = result.Value.Single(t => t.TableNumber == 1);
        var table2 = result.Value.Single(t => t.TableNumber == 2);

        table1.AvailableSlots[0].StartOffset.Should().Be(FutureDateAt(10, 0),
            because: "table 1 is the excluded reservation's table, so its 10:00–11:00 slots are freed");
        table2.AvailableSlots[0].StartOffset.Should().Be(FutureDateAt(11, 15),
            because: "table 2 belongs to a different reservation; exclusion does not apply to it");
    }
}
