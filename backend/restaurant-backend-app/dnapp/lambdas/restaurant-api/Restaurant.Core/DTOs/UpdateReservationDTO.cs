namespace Restaurant.Core.DTOs;

public class UpdateReservationDTO
{
    public string Id { get; set; }
    public int GuestNumber { get; set; }
    public string TableNumber { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
}