using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.DTOs;

public sealed record CreateReservationForWaiterDTO(
    string LocationId,
    int TableNumber,
    DateOnly Date,
    TimeOnly TimeFrom,
    TimeOnly TimeTo,
    int GuestsCount,
    string? CustomerId,
    string? VisitorName
);
