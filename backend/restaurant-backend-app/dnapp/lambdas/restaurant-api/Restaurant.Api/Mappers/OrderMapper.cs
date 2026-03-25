using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.Models;
using System.Text.Json;

namespace Restaurant.Api.Mappers;

public static class OrderMapper
{
    public static OrderResponse ToResponse(this Order x) => new()
    {
        Id = x.Id,
        ReservationId = x.ReservationId,
        LocationId = x.LocationId,
        LocationAddress = x.LocationAddress,
        WaiterName = x.WaiterName,
        CustomerName = x.CustomerName,
        VisitorName = x.VisitorName,
        TableNumber = x.TableNumber,
        GuestsCount = x.GuestsCount,
        Status = x.Status.ToString(),
        Dishes = x.Dishes,
        TotalAmount = x.TotalAmount,
        CreatedAt = x.CreatedAt,
        CompletedAt = x.CompletedAt
    };
}
