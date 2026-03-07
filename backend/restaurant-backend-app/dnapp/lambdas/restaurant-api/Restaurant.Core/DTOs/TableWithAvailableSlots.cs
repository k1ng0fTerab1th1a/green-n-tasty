namespace Restaurant.Core.DTOs;

public class TableWithAvailableSlots
{
    public required string LocationId { get; set; }
    public required int TableNumber { get; set; }
    public required string LocationAddress { get; set; }
    public required string LocationTimeZone { get; set; }
    public required int Capacity { get; set; }

    public required IList<TimeSlot> AvailableSlots { get; set; }
}

public class TimeSlot
{
    public required DateTimeOffset StartOffset { get; set; }
    public required DateTimeOffset EndOffset { get; set; }
}
