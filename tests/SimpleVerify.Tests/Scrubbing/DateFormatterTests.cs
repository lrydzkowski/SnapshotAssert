using SimpleVerify.Scrubbing;
using Xunit;

namespace SimpleVerify.Tests.Scrubbing;

public class DateFormatterTests
{
    [Fact]
    public void DateOnlyPart()
    {
        Assert.Equal("2020-01-01", DateFormatter.Convert(new DateTime(2020, 1, 1)));
    }

    [Fact]
    public void MinutePrecisionWithKind()
    {
        Assert.Equal(
            "2020-01-01 10:30 Utc",
            DateFormatter.Convert(new DateTime(2020, 1, 1, 10, 30, 0, DateTimeKind.Utc))
        );
    }

    [Fact]
    public void SecondPrecision()
    {
        Assert.Equal("2020-01-01 10:30:15", DateFormatter.Convert(new DateTime(2020, 1, 1, 10, 30, 15)));
    }

    [Fact]
    public void MillisecondPrecision()
    {
        Assert.Equal("2020-01-01 10:30:15.5", DateFormatter.Convert(new DateTime(2020, 1, 1, 10, 30, 15, 500)));
    }

    [Fact]
    public void OffsetWholeHours()
    {
        Assert.Equal(
            "2020-01-01 10:30 +2",
            DateFormatter.Convert(new DateTimeOffset(2020, 1, 1, 10, 30, 0, TimeSpan.FromHours(2)))
        );
    }

    [Fact]
    public void OffsetZero()
    {
        Assert.Equal(
            "2020-01-01 +0",
            DateFormatter.Convert(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero))
        );
    }
}
