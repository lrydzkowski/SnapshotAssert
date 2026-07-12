using System.Text;

namespace SimpleVerify.Scrubbing;

internal static class ApplyScrubbers
{
    public static void ApplyForExtension(StringBuilder target, VerifySettings settings, Counter counter)
    {
        foreach (Action<StringBuilder, Counter> scrubber in settings.InstanceScrubbers)
        {
            scrubber(target, counter);
        }

        DirectoryReplacements.Replace(target);
        target.FixNewlines();
    }

}
