using FluentResults;
using Microsoft.Extensions.Options;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;
using Restaurant.Core.SharedModels;

namespace Restaurant.Core.Services;

public class ReceiptService(
    IReservationRepository reservationRepository,
    IOrderRepository orderRepository,
    IReceiptPdfService pdfService,
    IOptions<ClientSettings> options) : IReceiptService
{
    public async Task<Result<byte[]>> GetReceiptAsync(string reservationId, string waiterId, CancellationToken ct)
    {
        var reservation = await reservationRepository.GetByIdAsync(reservationId, ct);
        if (reservation is null)
            return ReservationErrors.ReservationNotFound;

        if (!string.Equals(reservation.WaiterId, waiterId, StringComparison.Ordinal))
            return ReceiptErrors.Forbidden;

        if (reservation.Status != ReservationStatus.Finished)
            return ReceiptErrors.ReservationNotFinished;

        var order = await orderRepository.GetByReservationIdAsync(reservationId, ct);
        if (order is null)
            return ReceiptErrors.OrderNotFound;

        var isVisitor = reservation.CustomerId is null;
        var guestName = isVisitor ? reservation.VisitorName + " (Visitor)" : reservation.CustomerName ?? string.Empty;

        string? secretLink = null;
        if (isVisitor)
        {
            if (string.IsNullOrEmpty(reservation.SecretCode))
                return ReceiptErrors.VisitorSecretCodeMissing;

            secretLink = BuildAnonFeedbackLink(reservationId, reservation.SecretCode);
        }

        var dto = new ReceiptDTO
        {
            ReservationId    = reservation.Id,
            GuestName        = guestName,
            GuestIsVisitor   = isVisitor,
            SecretLink       = secretLink,
            WaiterName       = reservation.WaiterName,
            LocationAddress  = reservation.LocationAddress,
            TableNumber      = reservation.TableNumber,
            GuestsNumber     = reservation.GuestsCount,
            Dishes           = order.Dishes,
            TotalAmount      = order.TotalAmount,
            ActualStartTime  = reservation.ActualStartTime!,
            ActualEndTime    = reservation.ActualEndTime!,
        };

        return await pdfService.GeneratePdfAsync(dto, ct);
    }

    private string BuildAnonFeedbackLink(string reservationId, string secretCode) =>
        $"{options.Value.ClientUrl}/feedback?reservationId={reservationId}&secretCode={secretCode}";
}
