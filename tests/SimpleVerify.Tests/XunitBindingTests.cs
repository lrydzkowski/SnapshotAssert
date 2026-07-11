using System.Runtime.CompilerServices;
using SimpleVerify.Engine;
using Xunit;
using static SimpleVerify.Verifier;

namespace SimpleVerify.Tests;

public class XunitBindingTests
{
    private static string SourceDirectory([CallerFilePath] string path = "")
    {
        return Path.GetDirectoryName(path)!;
    }

    private static void DeleteReceived(string prefix)
    {
        string path = Path.Combine(SourceDirectory(), $"{prefix}.received.txt");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task EndToEndSnapshotPasses()
    {
        await Verify(new { Name = "John" });

        Assert.False(
            File.Exists(Path.Combine(SourceDirectory(), "XunitBindingTests.EndToEndSnapshotPasses.received.txt"))
        );
    }

    [Fact]
    public async Task VerifyJsonEndToEndSnapshotPasses()
    {
        await VerifyJson("{\"key\": \"value\"}");

        Assert.False(
            File.Exists(
                Path.Combine(SourceDirectory(), "XunitBindingTests.VerifyJsonEndToEndSnapshotPasses.received.txt")
            )
        );
    }

    [Fact]
    public async Task DefaultNamingDerivesFromClassAndMethod()
    {
        try
        {
            VerifyException exception = await Assert.ThrowsAsync<VerifyException>(async () => await Verify("content"));

            Assert.Contains("XunitBindingTests.DefaultNamingDerivesFromClassAndMethod.received.txt", exception.Message);
            Assert.Contains(SourceDirectory(), exception.Message);
        }
        finally
        {
            DeleteReceived("XunitBindingTests.DefaultNamingDerivesFromClassAndMethod");
        }
    }

    [Theory]
    [InlineData("001")]
    public async Task TheoryParameterSegmentUsesParameterName(string testCase)
    {
        try
        {
            VerifyException exception = await Assert.ThrowsAsync<VerifyException>(
                async () => await Verify("content").UseParameters(testCase)
            );

            Assert.Contains(
                "XunitBindingTests.TheoryParameterSegmentUsesParameterName_testCase=001.received.txt",
                exception.Message
            );
        }
        finally
        {
            DeleteReceived("XunitBindingTests.TheoryParameterSegmentUsesParameterName_testCase=001");
        }
    }

    [Theory]
    [InlineData(1)]
    public async Task TheoryWithoutUseParametersFailsWithGuidance(int testCase)
    {
        _ = testCase;
        VerifyException exception = await Assert.ThrowsAsync<VerifyException>(async () => await Verify("content"));

        Assert.Contains("UseParameters", exception.Message);
        Assert.Contains("UseFileName", exception.Message);
    }

    [Fact]
    public async Task UseFileNameOverridesThePrefix()
    {
        try
        {
            VerifyException exception = await Assert.ThrowsAsync<VerifyException>(
                async () => await Verify("content").UseFileName("CustomSnapshotName")
            );

            Assert.Contains("CustomSnapshotName.received.txt", exception.Message);
            Assert.DoesNotContain("UseFileNameOverridesThePrefix", exception.Message);
        }
        finally
        {
            DeleteReceived("CustomSnapshotName");
        }
    }

    [Fact]
    public async Task TooManyParameterValuesFails()
    {
        VerifyException exception = await Assert.ThrowsAsync<VerifyException>(
            async () => await Verify("content").UseParameters("a", "b")
        );

        Assert.Contains("has only 0 parameters", exception.Message);
    }

    [Fact]
    public async Task DuplicatePrefixIsRejected()
    {
        try
        {
            await Assert.ThrowsAsync<VerifyException>(
                async () => await Verify("content").UseFileName("DuplicatePrefixSnapshot")
            );

            VerifyException exception = await Assert.ThrowsAsync<VerifyException>(
                async () => await Verify("content").UseFileName("DuplicatePrefixSnapshot")
            );

            Assert.Contains("multiple verifications", exception.Message);
        }
        finally
        {
            DeleteReceived("DuplicatePrefixSnapshot");
        }
    }

    [Fact]
    public async Task SharedSettingsInstanceIsNotMutatedByUseParameters()
    {
        VerifySettings shared = new();

        VerifyException exception = await Assert.ThrowsAsync<VerifyException>(
            async () => await Verify("content", shared).UseFileName("SharedSettingsSnapshot")
        );

        Assert.Contains("SharedSettingsSnapshot", exception.Message);
        Assert.Null(shared.FileName);
        DeleteReceived("SharedSettingsSnapshot");
    }
}
