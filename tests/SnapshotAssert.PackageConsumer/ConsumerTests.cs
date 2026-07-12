namespace SnapshotAssert.PackageConsumer;

public class ConsumerTests
{
    [Fact]
    public async Task SnapshotRoundTrip()
    {
        var settings = new VerifySettings();
        settings.DontIgnoreEmptyCollections();

        await Verify(new { Name = "John", Age = 30, Tags = new List<string>() }, settings);
    }
}
