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

    [Fact]
    public void DateCountingIsEnabledByDefault()
    {
        VerifySettings settings = new();

        Assert.True(settings.EffectiveCountDates);
    }

    [Fact]
    public void DisableDateCountingFlipsEffectiveCountDates()
    {
        VerifySettings settings = new();

        settings.DisableDateCounting();

        Assert.False(settings.EffectiveCountDates);
    }

    [Fact]
    public void DisableDateCountingSurvivesParentCloneConstructor()
    {
        VerifySettings parent = new();
        parent.DisableDateCounting();

        VerifySettings clone = new(parent);

        Assert.False(clone.EffectiveCountDates);
    }
}
