using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.Models;
using System.Net;

namespace Restaurant.Api.Mappers
{
    public static class LocationMapper
    {
        public static LocationResponse ToResponse(this Location x) => new(
            x.Id,
            x.Address,
            x.TimeZone,
            x.OpenTime,
            x.CloseTime,
            x.Description,
            x.TotalCapacity,
            x.AverageOccupancy,
            x.ImageUrl,
            x.Rating
        );

    }
}
