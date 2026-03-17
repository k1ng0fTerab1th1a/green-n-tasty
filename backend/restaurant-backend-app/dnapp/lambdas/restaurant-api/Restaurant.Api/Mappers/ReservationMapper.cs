using Restaurant.Api.Contracts.Requests;
using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.Api.Mappers;

public static class ReservationMapper
{
    public static ReservationResponse ToResponse(this Reservation x) => new()
    {
        Id = x.Id,
        LocationId = x.LocationId,
        LocationAddress = x.LocationAddress,
        TableNumber = x.TableNumber,
        GuestsCount = x.GuestsCount,
        StartDateTime = x.StartDateTime,
        EndDateTime = x.EndDateTime,
        Status = x.Status.ToString(),
        IsCreatedByWaiter = x.IsCreatedByWaiter,
        VisitorName = x.VisitorName
    };

    public static CreateReservationDTO ToCreateDTO(this CreateReservationRequest x) => new(
        x.LocationId,
        x.TableNumber,
        DateOnly.Parse(x.Date),
        TimeOnly.Parse(x.TimeFrom),
        TimeOnly.Parse(x.TimeTo),
        x.GuestsCount
    );

    public static UpdateReservationDTO ToUpdateDTO(this UpdateReservationRequest x) => new(
        x.Id,
        x.GuestNumber,
        x.TableNumber,
        DateOnly.Parse(x.Date),
        TimeOnly.Parse(x.TimeFrom),
        TimeOnly.Parse(x.TimeTo)
    );
}
