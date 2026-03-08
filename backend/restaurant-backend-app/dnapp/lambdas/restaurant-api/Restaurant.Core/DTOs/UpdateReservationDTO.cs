namespace Restaurant.Core.DTOs;

public sealed record UpdateReservationDTO
{
    public UpdateReservationDTO(string Id, int GuestNumber, int TableNumber, string StartTime, string EndTime)
    {
        this.Id = Id;
        this.GuestNumber = GuestNumber;
        this.TableNumber = TableNumber;
        this.StartTime = StartTime;
        this.EndTime = EndTime;
    }

    public string Id { get; set; }
    public int GuestNumber { get; set; }
    public int TableNumber { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
}