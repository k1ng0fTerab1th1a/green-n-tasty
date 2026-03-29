using System.Globalization;

namespace Restaurant.Reports.Application.Exporters;

internal static class ExportFormatHelper
{
    public static string FormatDelta(decimal? delta)
    {
        if (delta is null)
            return "N/A";

        var sign = delta > 0 ? "+" : "";
        return $"{sign}{delta.Value.ToString("F2", CultureInfo.InvariantCulture)}%";
    }

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
