namespace Restaurant.Reports.Application.Builders;

internal static class ReportCalculationHelper
{
    public static decimal? CalculateDelta(decimal current, decimal? previous)
    {
        if (previous is null || previous == 0)
            return null;

        return Math.Round((current - previous.Value) / previous.Value * 100, 2);
    }
}
