using FluentAssertions;
using Moq;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.UnitTests.Services;

public sealed class TableServiceTests
{
    private static readonly DateOnly FutureDate = new(2030, 1, 15);

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
            EntityType = "LOCATION",
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

        result.Should().BeEmpty();
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

        result.Should().BeEmpty();
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

        result.Should().BeEmpty();
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
                "2030-01-15",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

        result.Should().HaveCount(1);
        result[0].TableNumber.Should().Be(1);
        result[0].AvailableSlots.Should().HaveCount(1);
        result[0].AvailableSlots[0].StartOffset.Should().Be(new DateTimeOffset(2030, 1, 15, 10, 0, 0, TimeSpan.Zero));
        result[0].AvailableSlots[0].EndOffset.Should().Be(new DateTimeOffset(2030, 1, 15, 22, 0, 0, TimeSpan.Zero));
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
                    Date = "2030-01-15",
                    ReservedSlots = new HashSet<string> { "2030-01-15T10:00+00:00" }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: new TimeOnly(10, 0), locationId: "loc1", capacity: null, ct: default);

        result.Should().BeEmpty();
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

        result.Should().HaveCount(1);
        result[0].TableNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetAvailableTablesAsync_WhenShiftHasShortFreeWindow_ShouldExcludeShortWindowFromSlots()
    {
        _tableRepo.Setup(r => r.GetByLocationIdAsync("loc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Table> { MakeTable("loc1", 1) }.AsReadOnly());
        // Shift 10:00–13:00. Reserving 10:45–11:30 creates:
        //   - free window 10:00–10:45 = 45 min -> below threshold, excluded from slots
        //   - free window 11:30–13:00 = 90 min -> qualifies, kept in slots
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
                    Date = "2030-01-15",
                    ReservedSlots = new HashSet<string>
                    {
                        "2030-01-15T10:45+00:00",
                        "2030-01-15T11:00+00:00",
                        "2030-01-15T11:15+00:00"
                    }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

        result.Should().HaveCount(1);
        result[0].AvailableSlots.Should().HaveCount(1, because: "only the 90-min window qualifies; the 45-min window is below the threshold");
        result[0].AvailableSlots[0].StartOffset.Should().Be(new DateTimeOffset(2030, 1, 15, 11, 30, 0, TimeSpan.Zero));
        result[0].AvailableSlots[0].EndOffset.Should().Be(new DateTimeOffset(2030, 1, 15, 13, 0, 0, TimeSpan.Zero));
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

        result.Should().BeEmpty();
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
                "2030-01-15",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: 4, ct: default);

        result.Should().HaveCount(1);
        result[0].TableNumber.Should().Be(2);
        result[0].Capacity.Should().Be(6);
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
                EntityType = "LOCATION",
                Address = "Address for loc-tbilisi",
                TimeZone = "Asia/Tbilisi",  // UTC+4, no DST
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
                "2030-01-15",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        _tableDayRepo.Setup(r => r.GetManyByTablesAndDateAsync(
                It.Is<IEnumerable<string>>(keys => keys.Contains("loc-tbilisi#1")),
                "2030-01-15",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc-tbilisi#1"] = new TableDay
                {
                    TableKey = "loc-tbilisi#1",
                    Date = "2030-01-15",
                    ReservedSlots = new HashSet<string> { "2030-01-15T13:00+04:00" }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: new TimeOnly(13, 0), locationId: null, capacity: null, ct: default);

        result.Should().HaveCount(1, because: "the Tbilisi table has 13:00 local reserved; the UTC table does not");
        result[0].LocationId.Should().Be("loc-utc");
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
                "2030-01-15",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>());

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: null, locationId: "loc1", capacity: null, ct: default);

        result.Should().HaveCount(1);
        result[0].AvailableSlots.Should().HaveCount(1, because: "the entire 12-hour shift is free — no split at midnight");
        result[0].AvailableSlots[0].StartOffset.Should().Be(new DateTimeOffset(2030, 1, 15, 14, 0, 0, TimeSpan.Zero));
        result[0].AvailableSlots[0].EndOffset.Should().Be(new DateTimeOffset(2030, 1, 16, 2, 0, 0, TimeSpan.Zero));
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
                "2030-01-15",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TableDay>
            {
                ["loc1#1"] = new TableDay
                {
                    TableKey = "loc1#1",
                    Date = "2030-01-15",
                    ReservedSlots = new HashSet<string> { "2030-01-16T01:00+00:00" }
                }
            });

        var result = await _sut.GetAvailableTablesAsync(FutureDate, time: new TimeOnly(1, 0), locationId: "loc1", capacity: null, ct: default);

        result.Should().BeEmpty();
    }
}
