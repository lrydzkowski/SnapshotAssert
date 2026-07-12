using System.Globalization;
using System.Text;
using SnapshotAssert.Scrubbing;
using SnapshotAssert.Serialization;
using Xunit;

namespace SnapshotAssert.Tests.Scrubbing;

public class ScrubberTests
{
    private static string Apply(VerifySettings settings, string input, Counter? counter = null)
    {
        counter ??= new Counter(true, true);
        StringBuilder builder = new(input);
        ApplyScrubbers.ApplyForExtension(builder, settings, counter);
        return builder.ToString();
    }

    [Fact]
    public void InlineGuidReplacedAndSharesCounterWithTypedValues()
    {
        VerifySettings settings = new();
        settings.ScrubInlineGuids();
        Counter counter = new(true, true);
        Guid guid = Guid.Parse("173535ae-995b-4cc6-a74e-8cd4be57039c");
        counter.Convert(guid);

        string result = Apply(settings, $"link: https://host/{guid}/details", counter);

        Assert.Equal("link: https://host/Guid_1/details", result);
    }

    [Fact]
    public void InlineGuidBoundedByLettersIsNotReplaced()
    {
        VerifySettings settings = new();
        settings.ScrubInlineGuids();

        string input = "x173535ae-995b-4cc6-a74e-8cd4be57039c";
        Assert.Equal(input, Apply(settings, input));
    }

    [Fact]
    public void BracedInlineGuidIsReplaced()
    {
        VerifySettings settings = new();
        settings.ScrubInlineGuids();

        string result = Apply(settings, "{173535ae-995b-4cc6-a74e-8cd4be57039c}");

        Assert.Equal("{Guid_1}", result);
    }

    [Fact]
    public void ScrubInlineGuidsAfterDontScrubGuidsThrows()
    {
        VerifySettings settings = new();
        settings.DontScrubGuids();

        Assert.Throws<InvalidOperationException>(settings.ScrubInlineGuids);
    }

    [Fact]
    public void InlineDateTimesReplacedUsingFormat()
    {
        VerifySettings settings = new();
        settings.ScrubInlineDateTimes("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        string result = Apply(settings, "started at 2020-01-02 03:04:05 and 2020-01-02 03:04:05 again");

        Assert.Equal("started at DateTime_1 and DateTime_1 again", result);
    }

    [Fact]
    public void InlineDateTimesWithMonthNames()
    {
        VerifySettings settings = new();
        settings.ScrubInlineDateTimes("dd MMMM yyyy", CultureInfo.InvariantCulture);

        string result = Apply(settings, "from 02 January 2020 to 15 September 2021.");

        Assert.Equal("from DateTime_1 to DateTime_2.", result);
    }

    [Fact]
    public void ScrubLinesContainingRemovesMatchingLinesCaseInsensitively()
    {
        VerifySettings settings = new();
        settings.ScrubLinesContaining("traceId");

        string result = Apply(settings, "keep\n  \"TRACEID\": \"abc\",\nalso keep");

        Assert.Equal("keep\nalso keep", result);
    }

    [Fact]
    public void ScrubLinesWithReplaceTransformsEveryLine()
    {
        VerifySettings settings = new();
        settings.ScrubLinesWithReplace(line => line.Contains("cs:line") ? line.Replace('\\', '/') : line);

        string result = Apply(settings, "at Method() in C:\\code\\File.cs:line 64\nunrelated");

        Assert.Equal("at Method() in C:/code/File.cs:line 64\nunrelated", result);
    }

    [Fact]
    public void ScrubLinesWithReplaceReturningNullRemovesLine()
    {
        VerifySettings settings = new();
        settings.ScrubLinesWithReplace(line => line.StartsWith("drop") ? null : line);

        string result = Apply(settings, "drop me\nkeep me");

        Assert.Equal("keep me", result);
    }

    [Fact]
    public void MostRecentlyRegisteredScrubberRunsFirst()
    {
        VerifySettings settings = new();
        settings.AddScrubber(builder => builder.Append("A"));
        settings.AddScrubber(builder => builder.Append("B"));

        Assert.Equal("xBA", Apply(settings, "x"));
    }

    [Fact]
    public void CarriageReturnsAreNormalized()
    {
        VerifySettings settings = new();

        Assert.Equal("a\nb\nc", Apply(settings, "a\r\nb\rc"));
    }

    [Fact]
    public void NonIdempotentScrubberAppliesExactlyOncePerVerification()
    {
        VerifySettings settings = new();
        settings.ScrubLinesWithReplace(line => line.Replace("a", "aa"));
        Counter counter = new(settings.EffectiveScrubDateTimes, settings.EffectiveScrubGuids);
        StringBuilder builder = JsonFormatter.AsJson(new { Text = "abc" }, settings, counter);

        ApplyScrubbers.ApplyForExtension(builder, settings, counter);

        Assert.Equal("{\n  Text: aabc\n}", builder.ToString());
    }
}
