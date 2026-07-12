using System.Text;

namespace SnapshotAssert.Scrubbing;

internal static class DirectoryReplacements
{
    private readonly record struct Pair(string Find, string Replace);

    private static volatile List<Pair> _items = BuildItems(null, null);

    public static void UseAssembly(string? solutionDirectory, string? projectDirectory)
    {
        _items = BuildItems(solutionDirectory, projectDirectory);
    }

    private static List<Pair> BuildItems(string? solutionDirectory, string? projectDirectory)
    {
        List<Pair> values = new();
        string baseDirectory = CleanPath(AppDomain.CurrentDomain.BaseDirectory);
        values.Add(new Pair(baseDirectory, "{CurrentDirectory}"));

        string currentDirectory = CleanPath(Environment.CurrentDirectory);
        if (currentDirectory != baseDirectory)
        {
            values.Add(new Pair(currentDirectory, "{CurrentDirectory}"));
        }

        values.Add(new Pair(CleanPath(Path.GetTempPath()), "{TempPath}"));

        string profileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profileDirectory))
        {
            values.Add(new Pair(CleanPath(profileDirectory), "{UserProfile}"));
        }

        if (projectDirectory is not null)
        {
            values.Add(new Pair(CleanPath(projectDirectory), "{ProjectDirectory}"));
        }

        if (solutionDirectory is not null)
        {
            string cleanedSolution = CleanPath(solutionDirectory);
            if (cleanedSolution.Length > 1)
            {
                values.Add(new Pair(cleanedSolution, "{SolutionDirectory}"));
            }
        }

        return values
            .DistinctBy(_ => _.Find)
            .OrderByDescending(_ => _.Find.Length)
            .ToList();
    }

    private static string CleanPath(string directory)
    {
        return directory.Replace('\\', '/').TrimEnd('/');
    }

    public static void Replace(StringBuilder builder)
    {
        if (builder.Length == 0)
        {
            return;
        }

        List<Pair> pairs = _items;
        if (builder.Length < pairs[^1].Find.Length)
        {
            return;
        }

        ReadOnlySpan<char> span = builder.ToString().AsSpan();
        List<(int Index, int Length, string Value)> matches = new();

        for (int index = 0; index < span.Length; index++)
        {
            foreach (Pair pair in pairs)
            {
                if (index + pair.Find.Length > span.Length)
                {
                    continue;
                }

                if (!TryMatchAt(span, index, pair.Find, out int matchLength))
                {
                    continue;
                }

                matches.Add((index, matchLength, pair.Replace));
                index += matchLength - 1;
                break;
            }
        }

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            (int index, int length, string value) = matches[i];
            builder.Overwrite(value, index, length);
        }
    }

    private static bool TryMatchAt(ReadOnlySpan<char> span, int position, string find, out int matchLength)
    {
        matchLength = 0;

        if (position > 0 && char.IsLetterOrDigit(span[position - 1]))
        {
            return false;
        }

        if (!IsPathMatchAt(span, position, find))
        {
            return false;
        }

        matchLength = find.Length;
        int trailingPosition = position + find.Length;
        if (trailingPosition >= span.Length)
        {
            return true;
        }

        char trailing = span[trailingPosition];
        if (char.IsLetterOrDigit(trailing))
        {
            return false;
        }

        if (trailing is '/' or '\\')
        {
            matchLength++;
        }

        return true;
    }

    private static bool IsPathMatchAt(ReadOnlySpan<char> span, int position, string find)
    {
        for (int i = 0; i < find.Length; i++)
        {
            char spanChar = span[position + i];
            char findChar = find[i];

            if (spanChar is '/' or '\\')
            {
                if (findChar != '/')
                {
                    return false;
                }

                continue;
            }

            if (spanChar != findChar)
            {
                return false;
            }
        }

        return true;
    }
}
