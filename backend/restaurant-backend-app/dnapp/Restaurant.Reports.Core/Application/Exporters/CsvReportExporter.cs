using Restaurant.Reports.Domain.Data;
using System.Globalization;
using System.Text;
using static Restaurant.Reports.Application.Exporters.ExportFormatHelper;

namespace Restaurant.Reports.Application.Exporters;

public class CsvReportExporter
{
    public Dictionary<string, byte[]> Export(FullReportData report)
    {
        var files = new Dictionary<string, byte[]>();

        files["location_comparison.csv"] = BuildLocationCsv(report.LocationReportData);
        files["waiter_comparison.csv"] = BuildWaiterCsv(report.WaiterReportData);

        return files;
    }

    public static byte[] BuildLocationCsv(IEnumerable<LocationReportData> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Location,Location address,Period Start,Period End," +
                      "Total Orders,Orders Delta," +
                      "Avg Cuisine Rating,Min Cuisine Rating,Rating Delta," +
                      "Revenue (USD),Revenue Delta");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                Escape(row.LocationId),
                Escape(row.LocationAddress),
                row.StartDate.ToString("dd.MM.yyyy"),
                row.EndDate.ToString("dd.MM.yyyy"),
                row.TotalOrders.ToString(CultureInfo.InvariantCulture),
                FormatDelta(row.OrderDelta),
                row.AvgCuisineRating.ToString("F2", CultureInfo.InvariantCulture),
                row.MinCuisineRating.ToString(CultureInfo.InvariantCulture),
                FormatDelta(row.CuisineRatingDelta),
                row.TotalRevenue.ToString("F2", CultureInfo.InvariantCulture),
                FormatDelta(row.RevenueDelta)
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] BuildWaiterCsv(IEnumerable<WaiterReportData> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Waiter,Email,Location Address,Period Start,Period End," +
                      "Working Hours,Orders Processed,Orders Delta," +
                      "Avg Service Rating,Min Service Rating,Rating Delta");

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                Escape(row.WaiterName),
                Escape(row.WaiterEmail),
                Escape(row.LocationAddress),
                row.StartDate.ToString("dd.MM.yyyy"),
                row.EndDate.ToString("dd.MM.yyyy"),
                row.WaiterWorkingHours.ToString(CultureInfo.InvariantCulture),
                row.OrdersProcessed.ToString(CultureInfo.InvariantCulture),
                FormatDelta(row.OrdersProcessedDelta),
                row.AvgServiceRating.ToString("F2", CultureInfo.InvariantCulture),
                row.MinServiceRating.ToString(CultureInfo.InvariantCulture),
                FormatDelta(row.AvgServiceRatingDelta)
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
