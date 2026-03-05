using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

namespace Restaurant.Core.Services;

public class TableService(
    ITableRepository _tableRepository, 
    ITableDayRepository _tableDayRepository,
    IConfiguration _config) : ITableService
{
    private const int SLOT_DURATION_MINUTES = 15;
    private const int IGNORE_SLOT_IF_LESS_THAN_MINUTES = 60;

    public async Task<IReadOnlyList<TableWithAvailableSlots>> GetAvailableTablesAsync(
        DateOnly date, 
        TimeOnly? time, 
        string? locationId, 
        int? capacity, 
        CancellationToken ct)
    {
        IReadOnlyList<Table> tables = string.IsNullOrWhiteSpace(locationId)
            ? await _tableRepository.GetAllAsync(ct)
            : await _tableRepository.GetByLocationIdAsync(locationId, ct);

        if (capacity.HasValue)
        {
            tables = tables.Where(t => t.Capacity >= capacity.Value).ToList();
        }

        if (!tables.Any())
            return new List<TableWithAvailableSlots>();

        string tzId = _config["RestaurantSettings:LocationsTimeZone"] ?? "Asia/Tbilisi"; // TODO: add to locations and tables
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        TimeOnly openTime = TimeOnly.Parse(_config["RestaurantSettings:RestaurantsOpenTime"] ?? "08:00");
        TimeOnly closeTime = TimeOnly.Parse(_config["RestaurantSettings:RestaurantsCloseTime"] ?? "23:30");

        DateTime shiftStartLocal = date.ToDateTime(openTime);
        DateTime shiftEndLocal = date.ToDateTime(closeTime);

        bool isNightShift = closeTime < openTime;
        if (isNightShift)
        {
            shiftEndLocal = shiftEndLocal.AddDays(1);
        }

        DateTime shiftStartUtc = TimeZoneInfo.ConvertTimeToUtc(shiftStartLocal, tz);
        DateTime shiftEndUtc = TimeZoneInfo.ConvertTimeToUtc(shiftEndLocal, tz);

        DateTime? requestedTimeUtc = null;
        if (time.HasValue)
        {
            var reqLocalDt = date.ToDateTime(time.Value);

            if (isNightShift && time.Value < openTime)
            {
                reqLocalDt = reqLocalDt.AddDays(1);
            }

            requestedTimeUtc = TimeZoneInfo.ConvertTimeToUtc(reqLocalDt, tz); // TODO: convert for each table using table.LocationTimeZone
        }

        List<TableWithAvailableSlots> result = new();
        DateTime nowUtc = DateTime.UtcNow;
        string dateStr = date.ToString("yyyy-MM-dd");

        foreach (Table table in tables)
        {
            string tableKey = $"{table.LocationId}#{table.TableNumber}";
            var tableDay = await _tableDayRepository.GetByTableAndDateAsync(tableKey, dateStr, ct);

            HashSet<string> reserved = tableDay?.ReservedSlots ?? new HashSet<string>();

            // If time is specified, do not process tables where the requested time is reserved
            if (requestedTimeUtc.HasValue && reserved.Contains(requestedTimeUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")))
                continue;

            var availableSlots = new List<TimeSlot>();
            var slotStartUtc = shiftStartUtc;
            TimeSlot? currentAvailableSlot = null;

            while (slotStartUtc < shiftEndUtc)
            {
                if (slotStartUtc < nowUtc) 
                    continue;

                var slotEndUtc = slotStartUtc.AddMinutes(SLOT_DURATION_MINUTES);

                bool slotIsFree = !reserved.Contains(slotStartUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"));

                if (slotIsFree)
                {
                    if (currentAvailableSlot == null)
                    {
                        currentAvailableSlot = new TimeSlot { 
                            StartUtc = slotStartUtc,
                            EndUtc = slotEndUtc 
                        };
                    }
                    else
                    {
                        currentAvailableSlot.EndUtc = slotEndUtc;
                    }
                }
                else
                {
                    if (currentAvailableSlot != null && 
                        currentAvailableSlot.IsAtLeastMinutes(IGNORE_SLOT_IF_LESS_THAN_MINUTES))
                    {
                        availableSlots.Add(currentAvailableSlot);
                        currentAvailableSlot = null;
                    }
                }

                slotStartUtc = slotEndUtc;
            }

            if (currentAvailableSlot != null && 
                currentAvailableSlot.IsAtLeastMinutes(IGNORE_SLOT_IF_LESS_THAN_MINUTES))
            {
                availableSlots.Add(currentAvailableSlot);
                currentAvailableSlot = null;
            }

            result.Add(new TableWithAvailableSlots
            {
                LocationId = table.LocationId,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                LocationAddress = table.LocationAddress,
                LocationTimeZone = tzId, // TODO: get from table
                AvailableSlots = availableSlots
            });
        }

        return result;
    }
}

static class TimeSlotExtensions
{
    internal static bool IsAtLeastMinutes(this TimeSlot timeSlot, int minutes)
    {
        return timeSlot.StartUtc.AddMinutes(minutes) <= timeSlot.EndUtc;
    }
}
