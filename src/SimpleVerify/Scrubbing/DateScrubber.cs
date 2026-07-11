using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace SimpleVerify.Scrubbing;

internal static class DateScrubber
{
    private static readonly ConcurrentDictionary<(string Format, string CultureName), (int Max, int Min)> LengthCache =
        new();

    public static Action<StringBuilder, Counter> BuildDateTimeScrubber(string format, CultureInfo? culture)
    {
        try
        {
            DateTime.MaxValue.ToString(format, culture);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                $"Format '{format}' is not valid for DateTime.ToString(format, culture).",
                exception
            );
        }

        return (builder, counter) => ReplaceDateTimes(builder, format, counter, culture ?? CultureInfo.CurrentCulture);
    }

    private static void ReplaceDateTimes(StringBuilder builder, string format, Counter counter, CultureInfo culture)
    {
        ReplaceInner(builder, format, counter, culture);
        if (TryGetFormatWithUpperMillisecondsTrimmed(format, out string trimmedFormat))
        {
            ReplaceInner(builder, trimmedFormat, counter, culture);
        }
    }

    private static bool TryGetFormatWithUpperMillisecondsTrimmed(string format, out string trimmedFormat)
    {
        foreach (string suffix in (string[])[".FFFF", ".FFF", ".FF", ".F"])
        {
            if (format.EndsWith(suffix, StringComparison.Ordinal))
            {
                trimmedFormat = format[..^suffix.Length];
                return true;
            }
        }

        trimmedFormat = string.Empty;
        return false;
    }

    private static void ReplaceInner(StringBuilder builder, string format, Counter counter, CultureInfo culture)
    {
        if (!counter.ScrubDateTimes)
        {
            return;
        }

        (int max, int min) = LengthCache.GetOrAdd((format, culture.Name), _ => ProbeLengths(format, culture));

        if (builder.Length < min)
        {
            return;
        }

        ReadOnlySpan<char> value = builder.ToString().AsSpan();
        int builderIndex = 0;
        for (int index = 0; index <= value.Length; index++)
        {
            bool found = false;
            for (int length = max; length >= min; length--)
            {
                int end = index + length;
                if (end > value.Length)
                {
                    continue;
                }

                ReadOnlySpan<char> slice = value.Slice(index, length);
                if (!DateTime.TryParseExact(slice, format, culture, DateTimeStyles.None, out DateTime date))
                {
                    continue;
                }

                string convert = counter.Convert(date);
                builder.Overwrite(convert, builderIndex, length);
                builderIndex += convert.Length;
                index += length - 1;
                found = true;
                break;
            }

            if (!found)
            {
                builderIndex++;
            }
        }
    }

    private static (int Max, int Min) ProbeLengths(string format, CultureInfo culture)
    {
        int min = int.MaxValue;
        int max = 0;
        foreach (DateTime probe in BuildProbeDates())
        {
            int length = probe.ToString(format, culture).Length;
            min = Math.Min(min, length);
            max = Math.Max(max, length);
        }

        return (max + 2, Math.Max(1, min - 2));
    }

    private static IEnumerable<DateTime> BuildProbeDates()
    {
        int[] years = [1, 999, 2000, 9999];
        int[] days = [1, 2, 3, 4, 5, 6, 7, 9, 10, 28];
        (int Hour, int Minute, int Second, int Millisecond)[] times =
        [
            (0, 0, 0, 0),
            (9, 9, 9, 0),
            (23, 59, 59, 0),
            (23, 59, 59, 999),
            (1, 2, 3, 45)
        ];
        DateTimeKind[] kinds = [DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local];

        foreach (int year in years)
        {
            foreach (int month in Enumerable.Range(1, 12))
            {
                foreach (int day in days)
                {
                    foreach ((int hour, int minute, int second, int millisecond) in times)
                    {
                        foreach (DateTimeKind kind in kinds)
                        {
                            yield return new DateTime(year, month, day, hour, minute, second, millisecond, kind);
                        }
                    }
                }
            }
        }
    }
}
