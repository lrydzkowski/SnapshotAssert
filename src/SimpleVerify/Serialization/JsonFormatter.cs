using System.Text;
using SimpleVerify.Scrubbing;
using SimpleVerify.Writing;

namespace SimpleVerify.Serialization;

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
