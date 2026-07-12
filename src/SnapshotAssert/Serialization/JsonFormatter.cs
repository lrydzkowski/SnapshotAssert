using System.Text;
using SnapshotAssert.Scrubbing;
using SnapshotAssert.Writing;

namespace SnapshotAssert.Serialization;

internal static class JsonFormatter
{
    public static StringBuilder AsJson(object? target, VerifySettings settings, Counter counter)
    {
        StringBuilder builder = new();
        VerifyTextWriter writer = new(builder);
        new ObjectWalker(settings, counter, writer).WriteTarget(target);
        builder.FixNewlines();
        return builder;
    }
}
