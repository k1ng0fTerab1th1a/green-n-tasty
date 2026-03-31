namespace Restaurant.Reports.Domain.Data;

public sealed record DateRange
{
    public DateTime From { get; }
    public DateTime To { get; }
    public int Days => (To - From).Days + 1;

    public DateRange(DateTime from, DateTime to)
    {
        From = from.Date;
        To = to.Date;
    }
}
