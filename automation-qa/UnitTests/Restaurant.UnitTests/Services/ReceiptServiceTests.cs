using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Options;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.Services;
using Restaurant.Core.SharedModels;

namespace Restaurant.UnitTests.Services;

public sealed class ReceiptServiceTests
{
    private readonly Mock<IReservationRepository> _reservationRepo;
    private readonly Mock<IOrderRepository> _orderRepo;
    private readonly Mock<IReceiptPdfService> _pdfService;
    private readonly ReceiptService _sut;

    public ReceiptServiceTests()
    {
        _reservationRepo = new Mock<IReservationRepository>(MockBehavior.Strict);
        _orderRepo       = new Mock<IOrderRepository>(MockBehavior.Strict);
        _pdfService      = new Mock<IReceiptPdfService>();
        _pdfService.Setup(p => p.GeneratePdfAsync(It.IsAny<ReceiptDTO>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(Result.Ok(new byte[] { 1, 2, 3 }));

        var options = Options.Create(new ClientSettings { ClientUrl = "http://test-url.com" });

        _sut = new ReceiptService(_reservationRepo.Object, _orderRepo.Object, _pdfService.Object, options);
    }

    // ────────────────────────────────────────────────────────
    //  GetReceiptAsync – guard checks
    // ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReceiptAsync_WhenReservationNotFound_ReturnsNotFoundError()
    {
        _reservationRepo
            .Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var result = await _sut.GetReceiptAsync("rsv-1", "waiter-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReservationErrors.ReservationNotFound);

        _reservationRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetReceiptAsync_WhenWaiterIsNotAssigned_ReturnsForbidden()
    {
        _reservationRepo
            .Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(waiterId: "waiter-2", status: ReservationStatus.Finished));

        var result = await _sut.GetReceiptAsync("rsv-1", "waiter-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReceiptErrors.Forbidden);

        _reservationRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetReceiptAsync_WhenReservationNotFinished_ReturnsValidationError()
    {
        _reservationRepo
            .Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(waiterId: "waiter-1", status: ReservationStatus.InProgress));

        var result = await _sut.GetReceiptAsync("rsv-1", "waiter-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReceiptErrors.ReservationNotFinished);

        _reservationRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetReceiptAsync_WhenOrderNotFound_ReturnsNotFoundError()
    {
        _reservationRepo
            .Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(waiterId: "waiter-1", status: ReservationStatus.Finished));

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _sut.GetReceiptAsync("rsv-1", "waiter-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReceiptErrors.OrderNotFound);

        _reservationRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _orderRepo.Verify(r => r.GetByReservationIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetReceiptAsync_WhenVisitorAndSecretCodeMissing_ReturnsValidationError()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.Finished, isVisitor: true, secretCode: null);

        _reservationRepo
            .Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildOrder());

        var result = await _sut.GetReceiptAsync("rsv-1", "waiter-1", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().Be(ReceiptErrors.VisitorSecretCodeMissing);

        _reservationRepo.Verify(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _orderRepo.Verify(r => r.GetByReservationIdAsync("rsv-1", It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepo.VerifyNoOtherCalls();
        _orderRepo.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
    }

    // ────────────────────────────────────────────────────────
    //  GetReceiptAsync – success paths
    // ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReceiptAsync_WhenCustomerReservation_ReturnsPdfBytes()
    {
        _reservationRepo
            .Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReservation(waiterId: "waiter-1", status: ReservationStatus.Finished));

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildOrder());

        var result = await _sut.GetReceiptAsync("rsv-1", "waiter-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _pdfService.Verify(p => p.GeneratePdfAsync(
            It.Is<ReceiptDTO>(dto => !dto.GuestIsVisitor && dto.SecretLink == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetReceiptAsync_WhenVisitorWithSecretCode_ReturnsPdfWithSecretLink()
    {
        var reservation = BuildReservation(waiterId: "waiter-1", status: ReservationStatus.Finished,
            isVisitor: true, secretCode: "CODE-42");

        _reservationRepo
            .Setup(r => r.GetByIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _orderRepo
            .Setup(r => r.GetByReservationIdAsync("rsv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildOrder());

        var result = await _sut.GetReceiptAsync("rsv-1", "waiter-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _pdfService.Verify(p => p.GeneratePdfAsync(
            It.Is<ReceiptDTO>(dto =>
                dto.GuestIsVisitor &&
                dto.SecretLink!.Contains("CODE-42") &&
                dto.SecretLink.Contains("rsv-1") &&
                dto.GuestName.Contains("(Visitor)")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────

    private static Reservation BuildReservation(
        string waiterId,
        ReservationStatus status,
        bool isVisitor = false,
        string? secretCode = "SECRET-1")
        => new()
        {
            Id              = "rsv-1",
            WaiterId        = waiterId,
            WaiterName      = "Alice",
            CustomerId      = isVisitor ? null : "customer-1",
            CustomerName    = isVisitor ? null : "John Smith",
            VisitorName     = isVisitor ? "Jane Doe" : null,
            LocationId      = "loc-1",
            LocationAddress = "42 Main St",
            TableNumber     = 5,
            TableKey        = "loc-1#5",
            GuestsCount     = 2,
            StartDateTime   = "2026-03-30T18:00:00Z",
            EndDateTime     = "2026-03-30T19:30:00Z",
            ActualStartTime = "2026-03-30T18:05:00Z",
            ActualEndTime   = "2026-03-30T19:28:00Z",
            Status          = status,
            SecretCode      = secretCode,
            CreatedAt       = "2026-03-29T10:00:00Z",
            UpdatedAt       = "2026-03-30T19:28:00Z",
        };

    private static Order BuildOrder()
        => new()
        {
            Id            = "order-1",
            ReservationId = "rsv-1",
            TotalAmount   = 47.50m,
            Dishes =
            [
                new OrderDishSnapshot { DishId = "d1", Name = "Margherita", PriceAtOrder = 12.50m, Quantity = 2 },
                new OrderDishSnapshot { DishId = "d2", Name = "Tiramisu",   PriceAtOrder = 7.50m,  Quantity = 1 },
            ],
            LocationId      = "loc-1",
            LocationAddress = "42 Main St",
            WaiterId        = "waiter-1",
            WaiterName      = "Alice",
            CreatedAt       = "2026-03-30T18:10:00Z",
        };
}
