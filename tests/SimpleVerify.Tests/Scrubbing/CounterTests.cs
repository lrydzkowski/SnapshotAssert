using SimpleVerify.Scrubbing;
using Xunit;

namespace SimpleVerify.Tests.Scrubbing;

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
}
