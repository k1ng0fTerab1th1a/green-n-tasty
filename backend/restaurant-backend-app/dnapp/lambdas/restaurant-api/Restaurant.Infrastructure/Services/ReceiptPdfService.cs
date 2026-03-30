using FluentResults;
using QRCoder;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Services;

namespace Restaurant.Infrastructure.Services;

public class ReceiptPdfService : IReceiptPdfService
{
    static ReceiptPdfService()
    {
        Settings.License = LicenseType.Community;
    }

    public Task<Result<byte[]>> GeneratePdfAsync(ReceiptDTO receipt, CancellationToken ct)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                // 80 mm thermal receipt width
                page.ContinuousSize(226, Unit.Point);
                page.Margin(12, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(3);

                    // ── Header ──────────────────────────────────────────────
                    col.Item().AlignCenter().Text("RECEIPT").Bold().FontSize(16);
                    col.Item().AlignCenter().Text(receipt.LocationAddress).FontSize(8);

                    Divider(col);

                    // ── Date / time ─────────────────────────────────────────
                    LabelValue(col, "Date:", FormatDate(receipt.ActualStartTime));
                    LabelValue(col, "Time:", $"{FormatTime(receipt.ActualStartTime)} – {FormatTime(receipt.ActualEndTime)}");

                    Divider(col);

                    // ── Guest / table info ──────────────────────────────────
                    LabelValue(col, "Guest:",  receipt.GuestName);
                    LabelValue(col, "Table:",  $"#{receipt.TableNumber}");
                    LabelValue(col, "Covers:", receipt.GuestsNumber.ToString());
                    LabelValue(col, "Waiter:", receipt.WaiterName);

                    Divider(col);

                    // ── Dishes header ───────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(4).Text("Item").Bold();
                        row.RelativeItem(1).AlignCenter().Text("Qty").Bold();
                        row.RelativeItem(2).AlignRight().Text("Price").Bold();
                    });
                    col.Item().LineHorizontal(0.25f);

                    // ── Dish lines ──────────────────────────────────────────
                    foreach (var dish in receipt.Dishes)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(4).Text(dish.Name);
                            row.RelativeItem(1).AlignCenter().Text(dish.Quantity.ToString());
                            row.RelativeItem(2).AlignRight().Text($"{dish.PriceAtOrder * dish.Quantity:F2}");
                        });

                        if (dish.Quantity > 1)
                        {
                            col.Item().PaddingLeft(6).Text($"@ {dish.PriceAtOrder:F2} each").FontSize(7).Italic();
                        }
                    }

                    Divider(col);

                    // ── Total ───────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text($"{receipt.TotalAmount:F2}").Bold().FontSize(11);
                    });

                    Divider(col);

                    // ── QR code (visitors only) ─────────────────────────────
                    if (receipt.GuestIsVisitor && !string.IsNullOrEmpty(receipt.SecretLink))
                    {
                        col.Item().PaddingTop(6).AlignCenter().Text("Scan to leave your feedback:").FontSize(8).Italic();
                        var qrBytes = GenerateQrCode(receipt.SecretLink);
                        col.Item().AlignCenter().Width(100, Unit.Point).Image(qrBytes);
                    }

                    // ── Footer ──────────────────────────────────────────────
                    col.Item().PaddingTop(8).AlignCenter().Text("Thank you for dining with us!").FontSize(8).Italic();
                    col.Item().AlignCenter().Text($"Reservation: {receipt.ReservationId}").FontSize(6);
                });
            });
        }).GeneratePdf();

        return Task.FromResult(Result.Ok(pdf));
    }

    private static byte[] GenerateQrCode(string url)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var code = new PngByteQRCode(data);
        return code.GetGraphic(pixelsPerModule: 10);
    }

    private static void Divider(ColumnDescriptor col) =>
        col.Item().PaddingVertical(2).LineHorizontal(0.5f);

    private static void LabelValue(ColumnDescriptor col, string label, string value) =>
        col.Item().Row(row =>
        {
            row.RelativeItem().Text(label);
            row.RelativeItem().AlignRight().Text(value);
        });

    private static string FormatDate(string iso) =>
        DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture)
            : iso;

    private static string FormatTime(string iso) =>
        DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)
            : iso;
}
