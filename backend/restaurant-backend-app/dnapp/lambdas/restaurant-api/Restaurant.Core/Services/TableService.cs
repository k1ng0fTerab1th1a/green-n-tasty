using FluentResults;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Helpers;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Interfaces.Services;
using Restaurant.Core.Models;

namespace Restaurant.Core.Services;

public class TableService(
    ITableRepository _tableRepository,
    ITableDayRepository _tableDayRepository,
    ILocationRepository _locationRepository,
    IReservationRepository _reservationRepository) : ITableService
{
    private const int SLOT_DURATION_MINUTES = 15;
    private const int IGNORE_SLOT_IF_LESS_THAN_MINUTES = 60;
    private const int FORBID_IF_IN_FUTURE_MORE_THAN_DAYS = 14;

    public async Task<Result<IList<TableWithAvailableSlots>>> GetAvailableTablesAsync(
        GetAvailableTablesQuery query,
        CancellationToken ct)
    {
        var (date, time, locationId, capacity, excludeReservationId) = query;

        int daysInFuture = date.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        if (daysInFuture < 0)
        {
            return TableErrors.RequestedSlotsFromPast;
        }
        if (daysInFuture > FORBID_IF_IN_FUTURE_MORE_THAN_DAYS)
        {
            return TableErrors.RequestedSlotsFromFarFuture;
        }

        IReadOnlyList<Table> tables = string.IsNullOrWhiteSpace(locationId)
            ? await _tableRepository.GetAllAsync(ct)
            : await _tableRepository.GetByLocationIdAsync(locationId, ct);

        if (capacity.HasValue)
        {
            tables = tables.Where(t => t.Capacity >= capacity.Value).ToList();
        }

        if (!tables.Any())
            return new List<TableWithAvailableSlots>();

        string? excludedTableKey = null;
        HashSet<string>? excludedSlots = null;
        if (!string.IsNullOrWhiteSpace(excludeReservationId))
        {
            Reservation? excludedReservation = await _reservationRepository.GetByIdAsync(excludeReservationId, ct);
            if (excludedReservation != null)
            {
                excludedTableKey = excludedReservation.TableKey;
                var start = DateTimeOffset.Parse(excludedReservation.StartDateTime);
                var end = DateTimeOffset.Parse(excludedReservation.EndDateTime);
                excludedSlots = ReservationTimeHelper.GenerateSlots(start, end).ToHashSet();
            }
        }

        List<TableWithAvailableSlots> result = new();

        foreach (var locationTables in tables.GroupBy(t => t.LocationId))
        {
            Table first = locationTables.First();
            var locId = first.LocationId;

            Location location = await _locationRepository.GetByIdAsync(locId, ct)
                ?? throw new InvalidDataException($"location with id {locId} not found but was specified in table #{first.TableNumber}");

            DateTime startLocal = date.ToDateTime(TimeOnly.Parse(location.OpenTime));
            DateTime endLocal = date.ToDateTime(TimeOnly.Parse(location.CloseTime));

            result.AddRange(await GetAvailableSlotsForLocationAsync(
                locationTables,
                date,
                TimeOnly.Parse(location.OpenTime),
                TimeOnly.Parse(location.CloseTime),
                time,
                TimeZoneInfo.FindSystemTimeZoneById(location.TimeZone),
                excludedTableKey,
                excludedSlots,
                ct
            ));
        }

        return result;
    }

    private async Task<IList<TableWithAvailableSlots>> GetAvailableSlotsForLocationAsync(
        IEnumerable<Table> tables,
        DateOnly date,
        TimeOnly openTime,
        TimeOnly closeTime,
        TimeOnly? requestedTime,
        TimeZoneInfo tz,
        string? excludedTableKey,
        HashSet<string>? excludedSlots,
        CancellationToken ct)
    {
        DateTime shiftStartLocal = date.ToDateTime(openTime);
        DateTime shiftEndLocal = date.ToDateTime(closeTime);

        bool isNightShift = shiftEndLocal < shiftStartLocal;
        if (isNightShift)
        {
            shiftEndLocal = shiftEndLocal.AddDays(1);
        }

        DateTimeOffset? reqTimeOffset = null;
        if (requestedTime.HasValue)
        {
            DateTime reqTimeLocal = date.ToDateTime(requestedTime.Value);

            // WARNING: the code below can lead to a slight misinterpretation: 
            // e.g. if a shift is from 2026-01-01 14:00 to 2026-01-02 02:00,
            // and the requested date and time are 2026-01-01 01:00,
            // reqTimeLocal will become 2026-01-02 01:00.
            if (isNightShift && reqTimeLocal < shiftStartLocal)
            {
                reqTimeLocal = reqTimeLocal.AddDays(1);
            }
            reqTimeOffset = new(reqTimeLocal, tz.GetUtcOffset(reqTimeLocal));
        }

        if (reqTimeOffset.HasValue && reqTimeOffset.Value < DateTimeOffset.UtcNow)
            return new List<TableWithAvailableSlots>();

        string dateStr = shiftStartLocal.ToString("yyyy-MM-dd");

        List<DateTimeOffset> slotsForShift = GenerateSlotsForShift(shiftStartLocal, shiftEndLocal, tz);
        List<TableWithAvailableSlots> result = new();

        var tableKeys = tables.Select(t => $"{t.LocationId}#{t.TableNumber}").ToList();
        IReadOnlyDictionary<string, TableDay> locationTableDays =
            await _tableDayRepository.GetManyByTablesAndDateAsync(tableKeys, dateStr, ct);

        foreach (Table table in tables)
        {
            string tableKey = $"{table.LocationId}#{table.TableNumber}";
            TableDay? tableDay = locationTableDays.GetValueOrDefault(tableKey);
            HashSet<string> reserved = tableDay?.ReservedSlots ?? new HashSet<string>();

            if (excludedSlots != null && tableKey == excludedTableKey)
            {
                reserved = new HashSet<string>(reserved);
                reserved.ExceptWith(excludedSlots);
            }

            if (reqTimeOffset.HasValue && reserved.Contains(reqTimeOffset.Value.ToString("yyyy-MM-ddTHH:mmzzz")))
                continue;

            List<TimeSlot> availableSlots = GenerateAvailableSlots(slotsForShift, reserved, tz);

            if (availableSlots.Count != 0)
            {
                result.Add(new TableWithAvailableSlots
                {
                    LocationId = table.LocationId,
                    TableNumber = table.TableNumber,
                    Capacity = table.Capacity,
                    LocationAddress = table.LocationAddress,
                    AvailableSlots = availableSlots
                });
            }
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
                        EndOffset = slotStartOffset
                    };
                }
                else
                {
                    currentAvailableSlot.EndOffset = slotStartOffset;
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
        }

        if (currentAvailableSlot != null &&
        currentAvailableSlot.IsAtLeastMinutes(IGNORE_SLOT_IF_LESS_THAN_MINUTES))
        {
            availableSlots.Add(currentAvailableSlot);
        }

        return availableSlots;
    }

    private static List<DateTimeOffset> GenerateSlotsForShift(DateTime startLocal, DateTime endLocal, TimeZoneInfo tz)
    {
        List<DateTimeOffset> slots = new();

        DateTimeOffset currentOffset = new(startLocal, tz.GetUtcOffset(startLocal));
        DateTimeOffset endOffset = new(endLocal, tz.GetUtcOffset(endLocal));

        while (currentOffset < endOffset)
        {
            slots.Add(currentOffset);

            currentOffset = currentOffset.AddMinutesWithTz(SLOT_DURATION_MINUTES, tz);
        }

        return slots;
    }
}

static class TimeExtensions
{
    internal static bool IsAtLeastMinutes(this TimeSlot timeSlot, int minutes)
    {
        return (timeSlot.EndOffset - timeSlot.StartOffset).TotalMinutes >= minutes;
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
