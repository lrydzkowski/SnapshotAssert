using System.Globalization;

namespace SnapshotAssert.Scrubbing;

internal static class DateFormatter
{
    public static string Convert(DateTime value)
    {
        string result = GetDatePart(value);

        if (value.Kind != DateTimeKind.Unspecified)
        {
            result += $" {value.Kind}";
        }

        return result;
    }

    public static string Convert(DateTimeOffset value)
    {
        return $"{GetDatePart(value.DateTime)} {GetDateOffset(value)}";
    }

    private static string GetDatePart(DateTime value)
    {
        if (value.TimeOfDay == TimeSpan.Zero)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (value is { Second: 0, Millisecond: 0 })
        {
            return value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        if (value.Millisecond == 0)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
    }

    private static string GetDateOffset(DateTimeOffset value)
    {
        TimeSpan offset = value.Offset;

        if (offset > TimeSpan.Zero)
        {
            if (offset.Minutes == 0)
            {
                return $"+{offset.TotalHours:0}";
            }

            return $"+{offset.Hours:0}-{offset.Minutes:00}";
        }

        if (offset < TimeSpan.Zero)
        {
            if (offset.Minutes == 0)
            {
                return $"{offset.Hours:0}";
            }

            return $"{offset.Hours:0}{offset.Minutes:00}";
        }

        return "+0";
    }
}
