using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DiffEngine;
using SnapshotAssert.Scrubbing;
using SnapshotAssert.Serialization;

namespace SnapshotAssert.Engine;

internal class InnerVerifier(string directory, string filePrefix, VerifySettings settings)
{
    private static readonly UTF8Encoding Encoding = new(true, true);

    private string ReceivedName => $"{filePrefix}.received.txt";

    private string VerifiedName => $"{filePrefix}.verified.txt";

    private string ReceivedPath => Path.Combine(directory, ReceivedName);

    private string VerifiedPath => Path.Combine(directory, VerifiedName);

    public Task Verify(object? target)
    {
        Counter counter = CreateCounter();
        StringBuilder builder;
        if (target is string stringTarget)
        {
            builder = new StringBuilder(stringTarget.Length == 0 ? "emptyString" : stringTarget);
        }
        else
        {
            builder = JsonFormatter.AsJson(target, settings, counter);
        }

        ApplyScrubbers.ApplyForExtension(builder, settings, counter);

        return CompareAndReport(builder.ToString());
    }

    public Task VerifyJson(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                $"VerifyJson requires valid JSON input: {exception.Message}",
                nameof(json),
                exception
            );
        }

        Counter counter = CreateCounter();
        StringBuilder builder = JsonFormatter.AsJson(node, settings, counter);
        ApplyScrubbers.ApplyForExtension(builder, settings, counter);

        return CompareAndReport(builder.ToString());
    }

    private Counter CreateCounter()
    {
        return new Counter(
            settings.EffectiveScrubDateTimes,
            settings.EffectiveScrubGuids,
            settings.EffectiveCountDates
        );
    }

    private async Task CompareAndReport(string received)
    {
        DeleteStaleReceivedFiles();

        if (!File.Exists(VerifiedPath) || new FileInfo(VerifiedPath).Length == 0)
        {
            await File.WriteAllTextAsync(ReceivedPath, received, Encoding).ConfigureAwait(false);
            await LaunchDiff().ConfigureAwait(false);
            throw VerifyException.New(directory, ReceivedName, VerifiedName, received);
        }

        string verified = await File.ReadAllTextAsync(VerifiedPath, Encoding).ConfigureAwait(false);
        if (verified.Contains('\r'))
        {
            throw new VerifyException(
                $"Verified file contains carriage returns: {VerifiedPath}. Verified files must use \\n line endings only."
            );
        }

        if (verified == received)
        {
            DiffRunner.Kill(ReceivedPath, VerifiedPath);
            return;
        }

        await File.WriteAllTextAsync(ReceivedPath, received, Encoding).ConfigureAwait(false);
        await LaunchDiff().ConfigureAwait(false);
        throw VerifyException.NotEqual(directory, ReceivedName, VerifiedName, received, verified);
    }

    private Task LaunchDiff()
    {
        if (DiffRunner.Disabled || BuildServerDetector.Detected)
        {
            return Task.CompletedTask;
        }

        return DiffRunner.LaunchForTextAsync(ReceivedPath, VerifiedPath, Encoding);
    }

    private void DeleteStaleReceivedFiles()
    {
        foreach (string file in Directory.EnumerateFiles(directory, $"{filePrefix}.received.*"))
        {
            File.Delete(file);
        }
    }
}
