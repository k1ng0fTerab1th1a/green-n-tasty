using FluentResults;
using Restaurant.Core.Errors;
using Restaurant.Core.Models;
using TimeZoneConverter;

namespace Restaurant.Core.Helpers;

internal static class ReservationTimeHelper
{
    internal static Result ValidateReservationTime(TimeOnly from, TimeOnly to, DateOnly date, Location location)
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
            return Result.Fail(ReservationErrors.OutsideWorkingHours(location.OpenTime, location.CloseTime));

        if (from.Minute % 15 != 0 || from.Second != 0)
            return Result.Fail(ReservationErrors.StartTimeNotAligned);

        if (to.Minute % 15 != 0 || to.Second != 0)
            return Result.Fail(ReservationErrors.EndTimeNotAligned);

        var duration = CalculateDuration(from, to);
        if (duration < 60)
            return Result.Fail(ReservationErrors.DurationTooShort);

        if (duration > 6 * 60)
            return Result.Fail(ReservationErrors.DurationTooLong);

        if (date.ToDateTime(from) <= DateTime.UtcNow)
            return Result.Fail(ReservationErrors.PastDateTime);

        return Result.Ok();
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
