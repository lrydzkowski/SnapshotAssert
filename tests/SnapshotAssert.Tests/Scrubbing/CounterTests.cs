using System.Globalization;
using SnapshotAssert.Scrubbing;
using Xunit;

namespace SnapshotAssert.Tests.Scrubbing;

public class CounterTests
{
    [Fact]
    public void SameGuidMapsToSameToken()
    {
        Counter counter = new(true, true);
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        Assert.Equal("Guid_1", counter.Convert(first));
        Assert.Equal("Guid_2", counter.Convert(second));
        Assert.Equal("Guid_1", counter.Convert(first));
    }

    [Fact]
    public void EmptyGuidHasDedicatedToken()
    {
        Counter counter = new(true, true);

        Assert.Equal("Guid_Empty", counter.Convert(Guid.Empty));
        Assert.Equal("Guid_1", counter.Convert(Guid.NewGuid()));
    }

    [Fact]
    public void GuidScrubbingCanBeDisabled()
    {
        Counter counter = new(true, false);

        Assert.False(counter.TryConvert(Guid.NewGuid(), out _));
    }

    [Fact]
    public void DateTimesNumberByFirstOccurrence()
    {
        Counter counter = new(true, true);
        DateTime first = new(2020, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        DateTime second = new(2021, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Assert.Equal("DateTime_1", counter.Convert(first));
        Assert.Equal("DateTime_2", counter.Convert(second));
        Assert.Equal("DateTime_1", counter.Convert(first));
    }

    [Fact]
    public void DateTimeKindDistinguishesValues()
    {
        Counter counter = new(true, true);
        DateTime utc = new(2020, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        DateTime local = new(2020, 6, 15, 10, 30, 0, DateTimeKind.Local);

        Assert.Equal("DateTime_1", counter.Convert(utc));
        Assert.Equal("DateTime_2", counter.Convert(local));
    }

    [Fact]
    public void MinAndMaxDatesHaveDedicatedTokens()
    {
        Counter counter = new(true, true);

        Assert.Equal("Date_MinValue", counter.Convert(DateTime.MinValue));
        Assert.Equal("Date_MaxValue", counter.Convert(DateTime.MaxValue));
        Assert.Equal("Date_MinValue", counter.Convert(default(DateTime)));
    }

    [Fact]
    public void DateTimeOffsetsGetOwnTokenFamily()
    {
        Counter counter = new(true, true);
        DateTimeOffset value = new(2020, 6, 15, 10, 30, 0, TimeSpan.FromHours(2));

        Assert.Equal("DateTimeOffset_1", counter.Convert(value));
    }

    [Fact]
    public void WholeStringGuidConverts()
    {
        Counter counter = new(true, true);
        Guid guid = Guid.NewGuid();

        Assert.True(counter.TryConvert(guid.ToString("D").AsSpan(), out string? result));
        Assert.Equal("Guid_1", result);
        Assert.Equal("Guid_1", counter.Convert(guid));
    }

    [Fact]
    public void WholeStringIsoDateTimeConvertsAsDateTimeOffsetFirst()
    {
        Counter counter = new(true, true);

        Assert.True(counter.TryConvert("2020-06-15T10:30:00".AsSpan(), out string? result));
        Assert.Equal("DateTimeOffset_1", result);
    }

    [Fact]
    public void WholeStringIsoDateTimeWithOffsetConverts()
    {
        Counter counter = new(true, true);

        Assert.True(counter.TryConvert("2020-06-15T10:30:00+02:00".AsSpan(), out string? result));
        Assert.Equal("DateTimeOffset_1", result);
    }

    [Fact]
    public void ArbitraryStringDoesNotConvert()
    {
        Counter counter = new(true, true);

        Assert.False(counter.TryConvert("not a guid or date".AsSpan(), out _));
    }

    [Fact]
    public void WholeStringIsoDateConverts()
    {
        Counter counter = new(true, true);

        Assert.True(counter.TryConvert("2026-07-12".AsSpan(), out string? result));
        Assert.Equal("DateTime_1", result);
    }

    [Fact]
    public void WholeStringInvariantShortDateConverts()
    {
        Counter counter = new(true, true);

        Assert.True(counter.TryConvert("07/12/2026".AsSpan(), out string? result));
        Assert.Equal("DateTime_1", result);
    }

    [Fact]
    public void IsoDateAndInvariantShortDateOfSameDayShareOneToken()
    {
        Counter counter = new(true, true);

        Assert.True(counter.TryConvert("2026-07-12".AsSpan(), out string? first));
        Assert.True(counter.TryConvert("07/12/2026".AsSpan(), out string? second));
        Assert.Equal(first, second);
    }

    [Fact]
    public void SingleDigitShortDateDoesNotConvert()
    {
        Counter counter = new(true, true);

        Assert.False(counter.TryConvert("7/12/2026".AsSpan(), out _));
    }

    [Fact]
    public void StringDateConversionIsIdenticalAcrossCultures()
    {
        string[] values = ["2026-07-12", "07/12/2026", "12.07.2026", "2020-06-15T10:30:00", "7/12/2026"];

        (bool Converted, string? Result)[] ConvertAll(string cultureName)
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);
                Counter counter = new(true, true);
                return values
                    .Select(value => (counter.TryConvert(value.AsSpan(), out string? result), result))
                    .ToArray();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        Assert.Equal(ConvertAll("en-US"), ConvertAll("de-DE"));
    }
}
