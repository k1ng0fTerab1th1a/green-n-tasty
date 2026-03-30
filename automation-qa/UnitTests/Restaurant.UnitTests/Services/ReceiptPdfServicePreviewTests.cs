using System.Diagnostics;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Services;

namespace Restaurant.UnitTests.Services;

public class ReceiptPdfServicePreviewTests
{
    [ManualFact]
    public async Task Preview_Receipt_Pdf()
    {
        var service = new ReceiptPdfService();
        var dto = new ReceiptDTO
        {
            ReservationId   = "res-123",
            GuestName       = "John Smith",
            GuestIsVisitor  = false,
            SecretLink      = null,
            WaiterName      = "Alice",
            LocationAddress = "42 Main St",
            TableNumber     = 5,
            GuestsNumber    = 2,
            TotalAmount     = 47.50m,
            ActualStartTime = "2026-03-30T18:00:00Z",
            ActualEndTime   = "2026-03-30T19:30:00Z",
            Dishes =
            [
                new OrderDishSnapshot { Name = "Margherita",  PriceAtOrder = 12.50m, Quantity = 2 },
                new OrderDishSnapshot { Name = "Tiramisu",    PriceAtOrder = 7.50m,  Quantity = 1 },
                new OrderDishSnapshot { Name = "Still Water", PriceAtOrder = 2.50m,  Quantity = 3 },
            ]
        };

        var result = await service.GeneratePdfAsync(dto, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var path = Path.Combine(Path.GetTempPath(), "receipt_preview.pdf");
        await File.WriteAllBytesAsync(path, result.Value);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    [ManualFact]
    public async Task Preview_Receipt_Pdf_Visitor_With_QrCode()
    {
        var service = new ReceiptPdfService();
        var dto = new ReceiptDTO
        {
            ReservationId   = "res-456",
            GuestName       = "Jane Doe (Visitor)",
            GuestIsVisitor  = true,
            SecretLink      = "https://example.com/feedback?reservationId=res-456&secretCode=abc123",
            WaiterName      = "Bob",
            LocationAddress = "7 Restaurant Lane",
            TableNumber     = 3,
            GuestsNumber    = 4,
            TotalAmount     = 83.00m,
            ActualStartTime = "2026-03-30T20:00:00Z",
            ActualEndTime   = "2026-03-30T21:45:00Z",
            Dishes =
            [
                new OrderDishSnapshot { Name = "Grilled Salmon", PriceAtOrder = 24.00m, Quantity = 2 },
                new OrderDishSnapshot { Name = "Caesar Salad",   PriceAtOrder = 9.50m,  Quantity = 2 },
                new OrderDishSnapshot { Name = "Sparkling Water",PriceAtOrder = 3.00m,  Quantity = 2 },
                new OrderDishSnapshot { Name = "Cheesecake",     PriceAtOrder = 7.50m,  Quantity = 1 },
            ]
        };

        var result = await service.GeneratePdfAsync(dto, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var path = Path.Combine(Path.GetTempPath(), "receipt_preview_visitor.pdf");
        await File.WriteAllBytesAsync(path, result.Value);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
