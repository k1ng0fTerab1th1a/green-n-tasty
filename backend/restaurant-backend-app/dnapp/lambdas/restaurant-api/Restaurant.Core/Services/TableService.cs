using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Restaurant.Core.Models;

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

        DateTimeOffset shiftStartOffset = new(shiftStartLocal, tz.GetUtcOffset(shiftStartLocal));
        DateTimeOffset shiftEndOffset = new(shiftEndLocal, tz.GetUtcOffset(shiftEndLocal));

        DateTime? requestedLocalDt = null;
        if (time.HasValue)
        {
            requestedLocalDt = date.ToDateTime(time.Value);

            if (isNightShift && time.Value < openTime)
            {
                requestedLocalDt = requestedLocalDt.Value.AddDays(1);
            }

            requestedTimeOffset = new(requestedLocalDt.Value, tz.GetUtcOffset(requestedLocalDt.Value)); // TODO: convert for each table using table.LocationTimeZone
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
            if (requestedTimeOffset.HasValue && reserved.Contains(requestedTimeOffset.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")))
                continue;

            var availableSlots = new List<TimeSlot>();
            var slotStartUtc = shiftStartOffset;
            TimeSlot? currentAvailableSlot = null;

            while (slotStartUtc < shiftEndUtc)
            {
                var slotEndUtc = slotStartUtc.AddMinutes(SLOT_DURATION_MINUTES);

                if (slotStartUtc < nowUtc)
                {
                    slotStartUtc = slotEndUtc;
                    continue;
                }

                bool slotIsFree = !reserved.Contains(slotStartUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"));

                if (slotIsFree)
                {
                    if (currentAvailableSlot == null)
                    {
                        currentAvailableSlot = new TimeSlot
                        {
                            StartOffset = slotStartUtc,
                            EndOffset = slotEndUtc
                        };
                    }
                    else
                    {
                        currentAvailableSlot.EndOffset = slotEndUtc;
                    }
                }
                else
                {
                    if (currentAvailableSlot != null &&
                        currentAvailableSlot.IsAtLeastMinutes(IGNORE_SLOT_IF_LESS_THAN_MINUTES))
                    {
                        availableSlots.Add(currentAvailableSlot);
                    }
                    currentAvailableSlot = null;
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
                LocationTimeZone = table.LocationTimeZone,
                AvailableSlots = availableSlots
            });
        }

        return result;
    }

    private static List<TimeSlot> GenerateAvailableSlots(
        IList<DateTimeOffset> allSlots, 
        HashSet<string> reserved,
        TimeZoneInfo tz)
    {
        List<TimeSlot> availableSlots = new();
        TimeSlot? currentAvailableSlot = null;
        DateTimeOffset nowOffset = DateTimeOffset.UtcNow;

        foreach (DateTimeOffset slotStartOffset in allSlots)
        {
            if (slotStartOffset < nowOffset)
                continue;
            
            DateTimeOffset slotEndOffset = slotStartOffset.AddMinutesWithTz(SLOT_DURATION_MINUTES, tz);

            bool slotIsFree = !reserved.Contains(slotStartOffset.ToString("yyyy-MM-ddTHH:mmzzz"));

            if (slotIsFree)
            {
                if (currentAvailableSlot == null)
                {
                    currentAvailableSlot = new TimeSlot
                    {
                        StartOffset = slotStartOffset,
                        EndOffset = slotEndOffset
                    };
                }
                else
                {
                    currentAvailableSlot.EndOffset = slotEndOffset;
                }
            }
            else
            {
                if (currentAvailableSlot != null &&
                    currentAvailableSlot.IsAtLeastMinutes(IGNORE_SLOT_IF_LESS_THAN_MINUTES, tz))
                {
                    availableSlots.Add(currentAvailableSlot);
                }
                currentAvailableSlot = null;
            }
        }

        if (currentAvailableSlot != null &&
        currentAvailableSlot.IsAtLeastMinutes(IGNORE_SLOT_IF_LESS_THAN_MINUTES, tz))
        {
            availableSlots.Add(currentAvailableSlot);
        }

        return availableSlots;
    }
}

static class TimeExtensions
{
    internal static bool IsAtLeastMinutes(this TimeSlot timeSlot, int minutes, TimeZoneInfo tz)
    {
        return timeSlot.StartOffset.AddMinutesWithTz(minutes, tz) <= timeSlot.EndOffset;
    }

    internal static DateTimeOffset AddMinutesWithTz(this DateTimeOffset dateTimeOffset, int minutes, TimeZoneInfo tz)
    {
        var resultUtc = dateTimeOffset.UtcDateTime.AddMinutes(minutes);
        return new DateTimeOffset(
            TimeZoneInfo.ConvertTimeFromUtc(resultUtc, tz),
            tz.GetUtcOffset(resultUtc)
        );
    }
}
