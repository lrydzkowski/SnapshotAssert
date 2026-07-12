using System.Globalization;
using SnapshotAssert.Engine;
using Xunit;

namespace SnapshotAssert.Tests.Engine;

public class InnerVerifierTests : IDisposable
{
    private readonly string _directory;

    public InnerVerifierTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "SnapshotAssert.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    private InnerVerifier CreateVerifier(string prefix = "Sample.Test", VerifySettings? settings = null)
    {
        return new InnerVerifier(_directory, prefix, settings ?? new VerifySettings());
    }

    private string ReceivedPath(string prefix = "Sample.Test")
    {
        return Path.Combine(_directory, $"{prefix}.received.txt");
    }

    private string VerifiedPath(string prefix = "Sample.Test")
    {
        return Path.Combine(_directory, $"{prefix}.verified.txt");
    }

    [Fact]
    public async Task FirstVerificationWritesReceivedAndThrows()
    {
        InnerVerifier verifier = CreateVerifier();

        VerifyException exception = await Assert.ThrowsAsync<VerifyException>(() => verifier.Verify("content"));

        Assert.Contains($"Directory: {_directory}", exception.Message);
        Assert.Contains("New:", exception.Message);
        Assert.Contains("Sample.Test.received.txt", exception.Message);
        Assert.Contains("Sample.Test.verified.txt", exception.Message);
        Assert.Contains("content", exception.Message);
        Assert.Equal("content", File.ReadAllText(ReceivedPath()));
    }

    [Fact]
    public async Task MatchingVerificationPassesAndLeavesNoReceivedFile()
    {
        File.WriteAllText(VerifiedPath(), "content");
        InnerVerifier verifier = CreateVerifier();

        await verifier.Verify("content");

        Assert.False(File.Exists(ReceivedPath()));
    }

    [Fact]
    public async Task MismatchWritesReceivedAndReportsBothContents()
    {
        File.WriteAllText(VerifiedPath(), "old value");
        InnerVerifier verifier = CreateVerifier();

        VerifyException exception = await Assert.ThrowsAsync<VerifyException>(() => verifier.Verify("new value"));

        Assert.Contains("NotEqual:", exception.Message);
        Assert.Contains("new value", exception.Message);
        Assert.Contains("old value", exception.Message);
        Assert.Equal("new value", File.ReadAllText(ReceivedPath()));
    }

    [Fact]
    public async Task VerifiedFileWithCarriageReturnsFails()
    {
        File.WriteAllText(VerifiedPath(), "line1\r\nline2");
        InnerVerifier verifier = CreateVerifier();

        VerifyException exception = await Assert.ThrowsAsync<VerifyException>(() => verifier.Verify("line1\nline2"));

        Assert.Contains("carriage returns", exception.Message);
        Assert.Contains(VerifiedPath(), exception.Message);
    }

    [Fact]
    public async Task StaleReceivedFileIsDeletedBeforeComparison()
    {
        File.WriteAllText(VerifiedPath(), "content");
        File.WriteAllText(ReceivedPath(), "stale");
        InnerVerifier verifier = CreateVerifier();

        await verifier.Verify("content");

        Assert.False(File.Exists(ReceivedPath()));
    }

    [Fact]
    public async Task EmptyStringTargetUsesEmptyStringConvention()
    {
        InnerVerifier verifier = CreateVerifier();

        await Assert.ThrowsAsync<VerifyException>(() => verifier.Verify(string.Empty));

        Assert.Equal("emptyString", File.ReadAllText(ReceivedPath()));
    }

    [Fact]
    public async Task EmptyVerifiedFileIsTreatedAsNew()
    {
        File.WriteAllText(VerifiedPath(), string.Empty);
        InnerVerifier verifier = CreateVerifier();

        VerifyException exception = await Assert.ThrowsAsync<VerifyException>(() => verifier.Verify("content"));

        Assert.Contains("New:", exception.Message);
    }

    [Fact]
    public async Task ObjectTargetRendersInVerifyFormat()
    {
        File.WriteAllText(VerifiedPath(), "{\n  Name: John,\n  Age: 30\n}");
        InnerVerifier verifier = CreateVerifier();

        await verifier.Verify(new { Name = "John", Age = 30 });

        Assert.False(File.Exists(ReceivedPath()));
    }

    [Fact]
    public async Task VerifyJsonRendersObjectGraphFormat()
    {
        File.WriteAllText(VerifiedPath(), "{\n  key: value\n}");
        InnerVerifier verifier = CreateVerifier();

        await verifier.VerifyJson("{\"key\": \"value\"}");

        Assert.False(File.Exists(ReceivedPath()));
    }

    [Fact]
    public async Task VerifyJsonWithInvalidInputFails()
    {
        InnerVerifier verifier = CreateVerifier();

        ArgumentException exception =
            await Assert.ThrowsAsync<ArgumentException>(() => verifier.VerifyJson("not json"));

        Assert.Contains("valid JSON", exception.Message);
    }

    [Fact]
    public async Task ScrubbersApplyToStringTargets()
    {
        File.WriteAllText(VerifiedPath(), "scrubbed-value");
        VerifySettings settings = new();
        settings.AddScrubber(builder => builder.Replace("secret", "scrubbed"));
        InnerVerifier verifier = CreateVerifier(settings: settings);

        await verifier.Verify("secret-value");

        Assert.False(File.Exists(ReceivedPath()));
    }

    [Fact]
    public async Task DisableDateCountingRendersAllDatesAsScrubbed()
    {
        File.WriteAllText(
            VerifiedPath(),
            "{\n  First: {Scrubbed},\n  Second: {Scrubbed},\n  Offset: {Scrubbed},\n  Text: {Scrubbed}\n}"
        );
        VerifySettings settings = new();
        settings.DisableDateCounting();
        InnerVerifier verifier = CreateVerifier(settings: settings);

        await verifier.Verify(
            new
            {
                First = new DateTime(2020, 6, 15, 10, 30, 0, DateTimeKind.Utc),
                Second = new DateTime(2021, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                Offset = new DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.FromHours(2)),
                Text = "2026-07-12"
            }
        );

        Assert.False(File.Exists(ReceivedPath()));
    }

    [Fact]
    public async Task DisableDateCountingAppliesToInlineDateScrubbing()
    {
        File.WriteAllText(VerifiedPath(), "started at {Scrubbed} done");
        VerifySettings settings = new();
        settings.DisableDateCounting();
        settings.ScrubInlineDateTimes("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        InnerVerifier verifier = CreateVerifier(settings: settings);

        await verifier.Verify("started at 2020-01-02 03:04:05 done");

        Assert.False(File.Exists(ReceivedPath()));
    }

    [Fact]
    public async Task DisableDateCountingWithDontScrubDateTimesRendersLiteralDates()
    {
        File.WriteAllText(VerifiedPath(), "{\n  Timestamp: 2020-06-15 10:30 Utc\n}");
        VerifySettings settings = new();
        settings.DontScrubDateTimes();
        settings.DisableDateCounting();
        InnerVerifier verifier = CreateVerifier(settings: settings);

        await verifier.Verify(new { Timestamp = new DateTime(2020, 6, 15, 10, 30, 0, DateTimeKind.Utc) });

        Assert.False(File.Exists(ReceivedPath()));
    }

    [Fact]
    public async Task ReceivedFilesAreWrittenUtf8WithBom()
    {
        InnerVerifier verifier = CreateVerifier();

        await Assert.ThrowsAsync<VerifyException>(() => verifier.Verify("content"));

        byte[] bytes = await File.ReadAllBytesAsync(ReceivedPath(), TestContext.Current.CancellationToken);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
    }

    [Fact]
    public async Task ReceivedFilesUseLineFeedOnly()
    {
        InnerVerifier verifier = CreateVerifier();

        await Assert.ThrowsAsync<VerifyException>(() => verifier.Verify("line1\r\nline2"));

        Assert.Equal("line1\nline2", File.ReadAllText(ReceivedPath()));
    }
}
