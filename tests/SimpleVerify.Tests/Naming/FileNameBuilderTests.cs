using SimpleVerify.Naming;
using Xunit;

namespace SimpleVerify.Tests.Naming;

public class FileNameBuilderTests
{
    [Fact]
    public void DateTimeParameterProducesValidFileName()
    {
        VerifySettings settings = new();
        settings.UseParameters(new DateTime(2026, 7, 12));

        string prefix = FileNameBuilder.Build("MyTests", "MyTest", ["when"], settings);

        Assert.Equal("MyTests.MyTest_when=07-12-2026 00-00-00", prefix);
    }

    [Fact]
    public void StringParameterWithSlashesIsSanitized()
    {
        VerifySettings settings = new();
        settings.UseParameters("a/b\\c");

        string prefix = FileNameBuilder.Build("MyTests", "MyTest", ["path"], settings);

        Assert.Equal("MyTests.MyTest_path=a-b-c", prefix);
    }

    [Fact]
    public void WildcardCharactersAreSanitized()
    {
        VerifySettings settings = new();
        settings.UseParameters("a*b?c");

        string prefix = FileNameBuilder.Build("MyTests", "MyTest", ["pattern"], settings);

        Assert.Equal("MyTests.MyTest_pattern=a-b-c", prefix);
    }

    [Fact]
    public void UseFileNameIsSanitized()
    {
        VerifySettings settings = new();
        settings.UseFileName("reports/2026:v1");

        string prefix = FileNameBuilder.Build("MyTests", "MyTest", [], settings);

        Assert.Equal("reports-2026-v1", prefix);
    }

    [Fact]
    public void PlainNamesAreUnchanged()
    {
        VerifySettings settings = new();
        settings.UseParameters("simple");

        string prefix = FileNameBuilder.Build("MyTests", "MyTest", ["name"], settings);

        Assert.Equal("MyTests.MyTest_name=simple", prefix);
    }
}
