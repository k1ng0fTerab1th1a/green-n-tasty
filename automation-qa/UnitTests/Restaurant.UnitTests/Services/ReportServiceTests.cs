using FluentAssertions;
using Moq;
using Restaurant.Core.Models;
using Restaurant.Reports.Application;
using Restaurant.Reports.Domain.Data;
using Restaurant.Reports.Domain.Entities;
using Restaurant.Reports.Domain.Errors;
using Restaurant.Reports.Infrastructure;
using System.Runtime.CompilerServices;

namespace Restaurant.UnitTests.Services;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GetLocationReportDataAsync_WhenDateRangeInvalid_ShouldReturnValidationError_AndNotCallRepository()
    {
        var repo = new Mock<IReportsRepository>(MockBehavior.Strict);
        var sut = new ReportService(repo.Object);

        var invalidRange = new DateRange(new DateTime(2026, 1, 5), new DateTime(2026, 1, 4));

        var result = await sut.GetLocationReportDataAsync(invalidRange, null, CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(ReportErrors.InvalidDateRange);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLocationReportDataAsync_WhenValidInput_ShouldAggregateData()
    {
        var repo = new Mock<IReportsRepository>(MockBehavior.Strict);
        var sut = new ReportService(repo.Object);

        var range = new DateRange(new DateTime(2026, 1, 10), new DateTime(2026, 1, 10));
        var entry = CreateEntry(
            reservationId: "r-1",
            locationId: "loc-1",
            waiterId: "waiter-1",
            revenue: 120m,
            serviceFeedback: 4,
            cuisineFeedback: 5);

        repo.Setup(r => r.QueryReportsByDateAsync(
                new DateTime(2026, 1, 10),
                new DateTime(2026, 1, 10),
            It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(entry));

        repo.Setup(r => r.GetAllLocationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Location>
            {
                ["loc-1"] = new Location { Id = "loc-1", Address = "Main street 1" }
            });

        var result = await sut.GetLocationReportDataAsync(range, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();

        var row = result.Value[0];
        row.LocationId.Should().Be("loc-1");
        row.LocationAddress.Should().Be("Main street 1");
        row.TotalOrders.Should().Be(1);
        row.TotalRevenue.Should().Be(120m);
        row.AvgCuisineRating.Should().Be(5m);
        row.MinCuisineRating.Should().Be(5);

        repo.Verify(r => r.QueryReportsByDateAsync(
            new DateTime(2026, 1, 10),
            new DateTime(2026, 1, 10),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetAllLocationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWaiterReportDataAsync_WhenValidInput_ShouldAggregateDataAndCalculateWorkingHours()
    {
        var repo = new Mock<IReportsRepository>(MockBehavior.Strict);
        var sut = new ReportService(repo.Object);

        var range = new DateRange(new DateTime(2026, 2, 2), new DateTime(2026, 2, 2));
        var entry = CreateEntry(
            reservationId: "r-2",
            locationId: "loc-2",
            waiterId: "w-2",
            revenue: 80m,
            serviceFeedback: 3,
            cuisineFeedback: 4);

        repo.Setup(r => r.QueryReportsByDateAsync(
                new DateTime(2026, 2, 2),
                new DateTime(2026, 2, 2),
            It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(entry));

        repo.Setup(r => r.GetAllWaitersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, User>
            {
                ["w-2"] = new User
                {
                    UserId = "w-2",
                    FirstName = "Ana",
                    LastName = "Smith",
                    Email = "ana@example.com"
                }
            });

        repo.Setup(r => r.GetAllLocationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Location>
            {
                ["loc-2"] = new Location
                {
                    Id = "loc-2",
                    Address = "Second street 2",
                    OpenTime = "09:00",
                    CloseTime = "17:00"
                }
            });

        repo.Setup(r => r.GetWaiterShiftCountsAsync(
                new DateTime(2026, 2, 2),
                new DateTime(2026, 2, 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["w-2"] = 2 });

        var result = await sut.GetWaiterReportDataAsync(range, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();

        var row = result.Value[0];
        row.WaiterId.Should().Be("w-2");
        row.WaiterName.Should().Be("Ana Smith");
        row.WaiterEmail.Should().Be("ana@example.com");
        row.LocationAddress.Should().Be("Second street 2");
        row.OrdersProcessed.Should().Be(1);
        row.AvgServiceRating.Should().Be(3m);
        row.MinServiceRating.Should().Be(3);
        row.WaiterWorkingHours.Should().Be(16);

        repo.Verify(r => r.QueryReportsByDateAsync(
            new DateTime(2026, 2, 2),
            new DateTime(2026, 2, 2),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetAllWaitersAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetAllLocationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetWaiterShiftCountsAsync(new DateTime(2026, 2, 2), new DateTime(2026, 2, 2), It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetFullReportDataAsync_WhenValidInput_ShouldReturnBothSections()
    {
        var repo = new Mock<IReportsRepository>(MockBehavior.Strict);
        var sut = new ReportService(repo.Object);

        var range = new DateRange(new DateTime(2026, 3, 15), new DateTime(2026, 3, 15));
        var entry = CreateEntry(
            reservationId: "r-3",
            locationId: "loc-3",
            waiterId: "w-3",
            revenue: 55m,
            serviceFeedback: 5,
            cuisineFeedback: 4);

        repo.Setup(r => r.QueryReportsByDateAsync(
                new DateTime(2026, 3, 15),
                new DateTime(2026, 3, 15),
            It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(entry));

        repo.Setup(r => r.GetAllLocationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Location>
            {
                ["loc-3"] = new Location { Id = "loc-3", Address = "Third street 3", OpenTime = "10:00", CloseTime = "18:00" }
            });

        repo.Setup(r => r.GetAllWaitersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, User>
            {
                ["w-3"] = new User { UserId = "w-3", FirstName = "Nina", LastName = "Lee", Email = "nina@example.com" }
            });

        repo.Setup(r => r.GetWaiterShiftCountsAsync(
                new DateTime(2026, 3, 15),
                new DateTime(2026, 3, 15),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["w-3"] = 1 });

        var result = await sut.GetFullReportDataAsync(range, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LocationReportData.Should().ContainSingle();
        result.Value.WaiterReportData.Should().ContainSingle();

        repo.Verify(r => r.QueryReportsByDateAsync(
            new DateTime(2026, 3, 15),
            new DateTime(2026, 3, 15),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetAllLocationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetAllWaitersAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetWaiterShiftCountsAsync(new DateTime(2026, 3, 15), new DateTime(2026, 3, 15), It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExportLocationReportAsync_WhenDataExists_ShouldReturnExcelBytes()
    {
        var repo = new Mock<IReportsRepository>(MockBehavior.Strict);
        var sut = new ReportService(repo.Object);

        var range = new DateRange(new DateTime(2026, 4, 1), new DateTime(2026, 4, 1));
        var entry = CreateEntry(
            reservationId: "r-4",
            locationId: "loc-4",
            waiterId: "w-4",
            revenue: 99m,
            serviceFeedback: 4,
            cuisineFeedback: 5);

        repo.Setup(r => r.QueryReportsByDateAsync(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 4, 1),
            It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(entry));

        repo.Setup(r => r.GetAllLocationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Location>
            {
                ["loc-4"] = new Location { Id = "loc-4", Address = "Fourth street 4" }
            });

        var result = await sut.ExportLocationReportAsync(range, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Take(2).Should().Equal((byte)'P', (byte)'K');

        repo.Verify(r => r.QueryReportsByDateAsync(
            new DateTime(2026, 4, 1),
            new DateTime(2026, 4, 1),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetAllLocationsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    private static ReportEntry CreateEntry(
        string reservationId,
        string locationId,
        string waiterId,
        decimal revenue,
        int? serviceFeedback,
        int? cuisineFeedback)
    {
        return new ReportEntry
        {
            ReservationId = reservationId,
            LocationId = locationId,
            Date = "2026-01-01",
            CompletedAt = "2026-01-01T12:00:00.0000000Z#" + reservationId,
            DurationMinutes = 60,
            WaiterId = waiterId,
            TotalRevenue = revenue,
            ServiceFeedback = serviceFeedback,
            CuisineFeedback = cuisineFeedback
        };
    }

    private static async IAsyncEnumerable<ReportEntry> ToAsyncEnumerable(
        ReportEntry entry,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return entry;
        await Task.CompletedTask;
    }
}
