using System.Globalization;

namespace SimpleVerify.Scrubbing;

internal class Counter(bool scrubDateTimes, bool scrubGuids)
{
    private sealed class DateTimeComparer : IEqualityComparer<DateTime>
    {
        public bool Equals(DateTime x, DateTime y)
        {
            return x == y && x.Kind == y.Kind;
        }

        public int GetHashCode(DateTime obj)
        {
            return obj.GetHashCode() + (int)obj.Kind;
        }
    }

    private sealed class DateTimeOffsetComparer : IEqualityComparer<DateTimeOffset>
    {
        public bool Equals(DateTimeOffset x, DateTimeOffset y)
        {
            return x == y && x.Offset == y.Offset;
        }

        public int GetHashCode(DateTimeOffset obj)
        {
            return obj.GetHashCode() + (int)obj.Offset.TotalMinutes;
        }
    }

    private const string DateTimeParseFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

    private readonly Dictionary<Guid, string> _guidCache = [];
    private readonly Dictionary<DateTime, string> _dateTimeCache = new(new DateTimeComparer());
    private readonly Dictionary<DateTimeOffset, string> _dateTimeOffsetCache = new(new DateTimeOffsetComparer());
    private int _currentGuid;
    private int _currentDateTime;
    private int _currentDateTimeOffset;

    public bool ScrubDateTimes { get; } = scrubDateTimes;

    public bool ScrubGuids { get; } = scrubGuids;

    public bool TryConvert(Guid value, out string? result)
    {
        if (!ScrubGuids)
        {
            result = null;
            return false;
        }

        result = Convert(value);
        return true;
    }

    public string Convert(Guid guid)
    {
        if (guid == Guid.Empty)
        {
            return "Guid_Empty";
        }

        return _guidCache.GetOrAdd(guid, _ => $"Guid_{++_currentGuid}");
    }

    public bool TryConvert(DateTime value, out string? result)
    {
        if (!ScrubDateTimes)
        {
            result = null;
            return false;
        }

        result = Convert(value);
        return true;
    }

    public string Convert(DateTime value)
    {
        if (value.Date == DateTime.MaxValue.Date)
        {
            return "Date_MaxValue";
        }

        if (value.Date == DateTime.MinValue.Date)
        {
            return "Date_MinValue";
        }

        return _dateTimeCache.GetOrAdd(value, _ => $"DateTime_{++_currentDateTime}");
    }

    public bool TryConvert(DateTimeOffset value, out string? result)
    {
        if (!ScrubDateTimes)
        {
            result = null;
            return false;
        }

        result = Convert(value);
        return true;
    }

    public string Convert(DateTimeOffset value)
    {
        if (value.Date == DateTime.MaxValue.Date)
        {
            return "Date_MaxValue";
        }

        if (value.Date == DateTime.MinValue.Date)
        {
            return "Date_MinValue";
        }

        return _dateTimeOffsetCache.GetOrAdd(value, _ => $"DateTimeOffset_{++_currentDateTimeOffset}");
    }

    public bool TryConvert(ReadOnlySpan<char> value, out string? result)
    {
        if (TryConvertGuid(value, out result)
            || TryConvertDateTimeOffset(value, out result)
            || TryConvertDateTime(value, out result)
            || TryConvertDate(value, out result))
        {
            return true;
        }

        result = null;
        return false;
    }

    private bool TryConvertGuid(ReadOnlySpan<char> value, out string? result)
    {
        if (ScrubGuids && Guid.TryParse(value, out Guid guid))
        {
            result = Convert(guid);
            return true;
        }

        result = null;
        return false;
    }

    private bool TryConvertDateTimeOffset(ReadOnlySpan<char> value, out string? result)
    {
        if (ScrubDateTimes
            && DateTimeOffset.TryParseExact(
                value,
                DateTimeParseFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTimeOffset date
            ))
        {
            result = Convert(date);
            return true;
        }

        result = null;
        return false;
    }

    private bool TryConvertDateTime(ReadOnlySpan<char> value, out string? result)
    {
        if (ScrubDateTimes
            && DateTime.TryParseExact(
                value,
                DateTimeParseFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date
            ))
        {
            result = Convert(date);
            return true;
        }

        result = null;
        return false;
    }

    private bool TryConvertDate(ReadOnlySpan<char> value, out string? result)
    {
        if (ScrubDateTimes
            && (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                || DateOnly.TryParseExact(value, "d", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)))
        {
            result = Convert(date.ToDateTime(TimeOnly.MinValue));
            return true;
        }

        result = null;
        return false;
    }
}

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> factory
    )
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out TValue? value))
        {
            return value;
        }

        value = factory(key);
        dictionary.Add(key, value);
        return value;
    }
}
