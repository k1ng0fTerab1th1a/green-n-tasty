using FluentAssertions;
using FluentResults;
using Moq;
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
    private readonly TableService _sut;

    public TableServiceTests()
    {
        _tableRepo = new Mock<ITableRepository>(MockBehavior.Strict);
        _tableDayRepo = new Mock<ITableDayRepository>(MockBehavior.Strict);
        _locationRepo = new Mock<ILocationRepository>(MockBehavior.Strict);
        _sut = new TableService(_tableRepo.Object, _tableDayRepo.Object, _locationRepo.Object);
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
            Rating = 4.0
        };

    private static Table MakeTable(string locationId, int tableNumber, int capacity = 4) =>
        new()
        {
            LocationId = locationId,
            TableNumber = tableNumber,
            LocationAddress = $"Address for {locationId}",
            Capacity = capacity
        };

    [Fact]
    public async Task GetAvailableTablesAsync_WhenLocationIdIsNull_ShouldCallGetAllAsync()
    {
        _tableRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table>().AsReadOnly());

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: null, capacity: null, ct: default);

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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: 4, ct: default);

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

        var act = async () => await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: new TimeOnly(10, 0), locationId: "loc1", capacity: null, ct: default);

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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: new TimeOnly(10, 0), locationId: "loc1", capacity: null, ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].TableNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenShiftHasShortFreeWindow_ShouldExcludeShortWindowFromSlots()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        // Shift 10:00–13:00. Reserving 11:15–12:00 (+15 min gap) creates:
        //   - free window 10:00–11:00 = 60 min -> qualifies, kept in slots
        //   - free window 12:15–12:45 = 30 min -> below threshold, excluded from slots
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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: 4, ct: default);

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
                Rating = 4.0
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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: new TimeOnly(13, 0), locationId: null, capacity: null, ct: default);

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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

        result.Value.Should().HaveCount(1);
        result.Value[0].AvailableSlots.Should().HaveCount(1, because: "the entire 12-hour shift is free — no split at midnight");
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

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: new TimeOnly(1, 0), locationId: "loc1", capacity: null, ct: default);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenDateIsInThePast_ShouldReturnFailedResult()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var result = await _sut.GetAvailableTablesAsync(pastDate, time: null, locationId: null, capacity: null, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<BusinessError>().Should().ContainSingle(e => e.Message.Contains("past"));
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenDateIsTooFarInFuture_ShouldReturnFailedResult()
    {
        var farFutureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15);

        var result = await _sut.GetAvailableTablesAsync(farFutureDate, time: null, locationId: null, capacity: null, ct: default);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<BusinessError>().Should().ContainSingle(e => e.Message.Contains("future"));
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

        var result = await _sut.GetAvailableTablesAsync(today, time: pastLocalTime, locationId: "loc1", capacity: null, ct: default);

        result.Value.Should().BeEmpty(because: "the requested time is in the past for this location's timezone");
        _tableDayRepo.VerifyNoOtherCalls();
    }
}
