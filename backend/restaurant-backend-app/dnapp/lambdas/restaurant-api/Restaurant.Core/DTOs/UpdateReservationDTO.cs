namespace Restaurant.Core.DTOs;

public sealed record UpdateReservationDTO
{
    public UpdateReservationDTO(string Id, int GuestNumber, int TableNumber, DateOnly Date, TimeOnly TimeFrom,
        TimeOnly TimeTo)
    {
        this.Id = Id;
        this.GuestNumber = GuestNumber;
        this.TableNumber = TableNumber;
        this.Date = Date;
        this.TimeFrom = TimeFrom;
        this.TimeTo = TimeTo;
    }

    public string Id { get; set; }
    public int GuestNumber { get; set; }
    public int TableNumber { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly TimeFrom { get; set; }
    public TimeOnly TimeTo { get; set; }
}