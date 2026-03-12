using Restaurant.Core.Exceptions;
using Restaurant.Core.Models;
using TimeZoneConverter;

namespace Restaurant.Core.Helpers;

internal static class ReservationTimeHelper
{
    internal static void ValidateReservationTime(TimeOnly from, TimeOnly to, DateOnly date, Location location)
    {
        var openTime = TimeOnly.Parse(location.OpenTime);
        var closeTime = TimeOnly.Parse(location.CloseTime);
        var crossMidnight = closeTime < openTime;

        var fromValid = crossMidnight
            ? from >= openTime || from < closeTime
            : from >= openTime && from < closeTime;

        var toValid = crossMidnight
            ? to > openTime || to <= closeTime
            : to > openTime && to <= closeTime;

        if (!fromValid || !toValid)
            throw new BusinessException($"Reservation must be within working hours ({location.OpenTime} - {location.CloseTime}).");

        if (from.Minute % 15 != 0 || from.Second != 0)
            throw new BusinessException("Start time must be a multiple of 15 minutes.");

        if (to.Minute % 15 != 0 || to.Second != 0)
            throw new BusinessException("End time must be a multiple of 15 minutes.");

        var duration = CalculateDuration(from, to);
        if (duration < 60)
            throw new BusinessException("Minimum booking duration is 60 minutes.");

        if (duration > 6 * 60)
            throw new BusinessException("Maximum booking duration is 6 hours.");

        if (date.ToDateTime(from) <= DateTime.UtcNow)
            throw new BusinessException("Cannot book for a past date or time.");
    }

    internal static int CalculateDuration(TimeOnly from, TimeOnly to)
    {
        if (to >= from)
            return (int)(to - from).TotalMinutes;

        var toMidnight = (int)(TimeOnly.MaxValue - from).TotalMinutes + 1;
        var fromMidnight = to.Hour * 60 + to.Minute;
        return toMidnight + fromMidnight;
    }

    internal static List<string> GenerateSlots(DateTimeOffset start, DateTimeOffset end)
    {
        var slots = new List<string>();
        var cursor = start;

        while (true)
        {
            slots.Add(cursor.ToString("yyyy-MM-ddTHH:mmzzz"));
            if (cursor >= end) break;
            cursor = cursor.AddMinutes(15);
        }

        return slots;
    }

    internal static DateTimeOffset ToDateTimeOffset(DateOnly date, TimeOnly time, string timeZoneId)
    {
        var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
        var dt = date.ToDateTime(time);
        return new DateTimeOffset(dt, tz.GetUtcOffset(dt));
    }

    internal static (DateOnly StartDate, DateOnly EndDate) ResolveReservationDates(
        DateOnly businessDate,
        TimeOnly from,
        TimeOnly to,
        Location location)
    {
        var openTime = TimeOnly.Parse(location.OpenTime);
        var closeTime = TimeOnly.Parse(location.CloseTime);
        var crossMidnight = closeTime < openTime;

        var startDate = businessDate;
        if (crossMidnight && from < closeTime)
            startDate = startDate.AddDays(1);

        var endDate = to < from ? startDate.AddDays(1) : startDate;
        return (startDate, endDate);
    }
}
