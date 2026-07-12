using System.Text;
using SnapshotAssert.Scrubbing;
using Xunit;

namespace SnapshotAssert.Tests.Scrubbing;

public class DirectoryReplacementsTests
{
    private static string Replace(string input)
    {
        StringBuilder builder = new(input);
        DirectoryReplacements.Replace(builder);
        return builder.ToString();
    }

    [Fact]
    public void SolutionAndProjectDirectoriesAreReplaced()
    {
        DirectoryReplacements.UseAssembly("Q:/fake-solution", "Q:/fake-solution/src/Fake.Project");

        string result = Replace(
            "at Handler() in Q:\\fake-solution\\src\\File.cs:line 64\n"
            + "config Q:/fake-solution/src/Fake.Project/app.json"
        );

        Assert.Equal(
            "at Handler() in {SolutionDirectory}src\\File.cs:line 64\n" + "config {ProjectDirectory}app.json",
            result
        );
    }

    [Fact]
    public void SeparatorRewrittenPathsStillMatch()
    {
        DirectoryReplacements.UseAssembly("Q:/fake-solution", "Q:/fake-solution/src/Fake.Project");

        string result = Replace("in Q:/fake-solution/src/File.cs:line 50");

        Assert.Equal("in {SolutionDirectory}src/File.cs:line 50", result);
    }

    [Fact]
    public void PathPrecededByLetterIsNotReplaced()
    {
        DirectoryReplacements.UseAssembly("Q:/fake-solution", "Q:/fake-solution/src/Fake.Project");

        string input = "xQ:/fake-solution/src";
        Assert.Equal(input, Replace(input));
    }

    [Fact]
    public void TempPathIsReplaced()
    {
        DirectoryReplacements.UseAssembly(null, null);
        string tempFile = Path.Combine(Path.GetTempPath(), "some.txt");

        Assert.Equal("file: {TempPath}some.txt", Replace($"file: {tempFile}"));
    }
}
