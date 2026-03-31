using ClosedXML.Excel;
using Restaurant.Reports.Domain.Data;
using static Restaurant.Reports.Application.Exporters.ExportFormatHelper;

namespace Restaurant.Reports.Application.Exporters;

public class ExcelReportExporter
{
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#2F5496");
    private static readonly XLColor HeaderFont = XLColor.White;
    private static readonly XLColor AltRowBg = XLColor.FromHtml("#D6E4F0");
    private static readonly XLColor PositiveDelta = XLColor.FromHtml("#548235");
    private static readonly XLColor NegativeDelta = XLColor.FromHtml("#C00000");

    public byte[] Export(FullReportData reportData)
    {
        using var workbook = new XLWorkbook();

        AddLocationSheet(workbook, reportData.LocationReportData);
        AddWaiterSheet(workbook, reportData.WaiterReportData);

        return SaveWorkbook(workbook);
    }

    public static void AddLocationSheet(XLWorkbook workbook, List<LocationReportData> data)
    {
        var ws = workbook.AddWorksheet("Locations");

        var headers = new[]
        {
            "Location", "Location address", "Period Start", "Period End",
            "Total Orders", "Orders Delta",
            "Avg Cuisine Rating", "Min Cuisine Rating", "Rating Delta",
            "Revenue (USD)", "Revenue Delta"
        };

        // --- Header row ---
        var headerRow = ws.Row(1);
        headerRow.Height = 30;
        for (int col = 0; col < headers.Length; col++)
        {
            var cell = ws.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = HeaderFont;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }

        // --- Data rows ---
        for (int i = 0; i < data.Count; i++)
        {
            var row = data[i];
            int r = i + 2;

            ws.Cell(r, 1).Value = row.LocationId;
            ws.Cell(r, 2).Value = row.LocationAddress ?? "";
            ws.Cell(r, 3).Value = row.StartDate.ToString("dd.MM.yyyy");
            ws.Cell(r, 4).Value = row.EndDate.ToString("dd.MM.yyyy");
            ws.Cell(r, 5).Value = row.TotalOrders;
            SetDeltaCell(ws.Cell(r, 6), row.OrderDelta);
            ws.Cell(r, 7).Value = (double)row.AvgCuisineRating;
            ws.Cell(r, 8).Value = row.MinCuisineRating;
            SetDeltaCell(ws.Cell(r, 9), row.CuisineRatingDelta);
            ws.Cell(r, 10).Value = (double)row.TotalRevenue;
            SetDeltaCell(ws.Cell(r, 11), row.RevenueDelta);

            // Alternate row shading
            if (i % 2 == 1)
            {
                var rowRange = ws.Range(r, 1, r, headers.Length);
                rowRange.Style.Fill.BackgroundColor = AltRowBg;
            }
        }

        // --- Number formats ---
        var lastRow = data.Count + 1;
        if (lastRow >= 2)
        {
            ws.Range(2, 7, lastRow, 7).Style.NumberFormat.Format = "0.00";   // Avg rating
            ws.Range(2, 10, lastRow, 10).Style.NumberFormat.Format = "#,##0.00"; // Revenue
        }

        // --- Borders on all data ---
        var fullRange = ws.Range(1, 1, lastRow, headers.Length);
        fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        fullRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        fullRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#8DB4E2");
        fullRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#8DB4E2");

        // --- Freeze header row ---
        ws.SheetView.FreezeRows(1);

        // --- Auto-fit columns ---
        ws.Columns().AdjustToContents();

        // Min width for narrow columns
        foreach (var col in ws.ColumnsUsed())
        {
            if (col.Width < 10)
                col.Width = 10;
        }
    }

    private static byte[] SaveWorkbook(XLWorkbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public static void AddWaiterSheet(XLWorkbook workbook, List<WaiterReportData> data)
    {
        var ws = workbook.AddWorksheet("Waiters");

        var headers = new[]
        {
            "Waiter", "Email", "Location Address", "Period Start", "Period End",
            "Working Hours", "Orders Processed", "Orders Delta",
            "Avg Service Rating", "Min Service Rating", "Rating Delta"
        };

        var headerRow = ws.Row(1);
        headerRow.Height = 30;
        for (int col = 0; col < headers.Length; col++)
        {
            var cell = ws.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = HeaderFont;
            cell.Style.Fill.BackgroundColor = HeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }

        for (int i = 0; i < data.Count; i++)
        {
            var row = data[i];
            int r = i + 2;

            ws.Cell(r, 1).Value = row.WaiterName;
            ws.Cell(r, 2).Value = row.WaiterEmail;
            ws.Cell(r, 3).Value = row.LocationAddress ?? "";
            ws.Cell(r, 4).Value = row.StartDate.ToString("dd.MM.yyyy");
            ws.Cell(r, 5).Value = row.EndDate.ToString("dd.MM.yyyy");
            ws.Cell(r, 6).Value = row.WaiterWorkingHours;
            ws.Cell(r, 7).Value = row.OrdersProcessed;
            SetDeltaCell(ws.Cell(r, 8), row.OrdersProcessedDelta);
            ws.Cell(r, 9).Value = (double)row.AvgServiceRating;
            ws.Cell(r, 10).Value = row.MinServiceRating;
            SetDeltaCell(ws.Cell(r, 11), row.AvgServiceRatingDelta);

            if (i % 2 == 1)
            {
                var rowRange = ws.Range(r, 1, r, headers.Length);
                rowRange.Style.Fill.BackgroundColor = AltRowBg;
            }
        }

        var lastRow = data.Count + 1;
        if (lastRow >= 2)
        {
            ws.Range(2, 9, lastRow, 9).Style.NumberFormat.Format = "0.00";
        }

        var fullRange = ws.Range(1, 1, lastRow, headers.Length);
        fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        fullRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        fullRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#8DB4E2");
        fullRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#8DB4E2");

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        foreach (var col in ws.ColumnsUsed())
        {
            if (col.Width < 10)
                col.Width = 10;
        }
    }

    private static void SetDeltaCell(IXLCell cell, decimal? delta)
    {
        cell.Value = FormatDelta(delta);

        if (delta is > 0)
            cell.Style.Font.FontColor = PositiveDelta;
        else if (delta is < 0)
            cell.Style.Font.FontColor = NegativeDelta;
    }
}
