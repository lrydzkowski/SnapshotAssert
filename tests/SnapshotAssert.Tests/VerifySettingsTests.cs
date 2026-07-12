using Xunit;

namespace SnapshotAssert.Tests;

public class VerifySettingsTests
{
    [Fact]
    public void UseParametersWithNullArrayThrowsDescriptiveException()
    {
        VerifySettings settings = new();

        Assert.Throws<ArgumentNullException>("parameters", () => settings.UseParameters(null!));
    }
}
