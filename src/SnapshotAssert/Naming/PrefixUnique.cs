using System.Collections.Concurrent;
using SnapshotAssert.Engine;

namespace SnapshotAssert.Naming;

internal static class PrefixUnique
{
    private static readonly ConcurrentDictionary<string, byte> UsedPrefixes = new(StringComparer.OrdinalIgnoreCase);

    public static void CheckPrefixIsUnique(string prefix)
    {
        if (!UsedPrefixes.TryAdd(prefix, 0))
        {
            throw new VerifyException(
                $"The snapshot file prefix is used by multiple verifications: {prefix}. Use UseParameters(...) or UseFileName(...) to make each verification unique."
            );
        }
    }
}
