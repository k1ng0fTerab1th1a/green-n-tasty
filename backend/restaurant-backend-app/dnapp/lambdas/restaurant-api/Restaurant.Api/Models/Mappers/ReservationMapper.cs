using Restaurant.Api.Models.Responses.Reservations;
using Restaurant.Core.Models;

namespace Restaurant.Api.Models.Mappers
{
    public static class ReservationMapper
    {
        public static ReservationResponse ToResponse(this Reservation x) => new()
        {
            Id = x.Id,
            LocationId = x.LocationId,
            TableNumber = x.TableNumber,
            GuestsCount = x.GuestsCount,
            StartDateTime = x.StartDateTime,
            EndDateTime = x.EndDateTime,
            Status = x.Status.ToString()
        };
    }
}
