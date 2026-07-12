using System.Text;

namespace SnapshotAssert.Scrubbing;

internal static class StringBuilderScrubbingExtensions
{
    public static void FixNewlines(this StringBuilder builder)
    {
        builder.Replace("\r\n", "\n");
        builder.Replace('\r', '\n');
    }

    public static void Overwrite(this StringBuilder builder, string value, int index, int length)
    {
        builder.Remove(index, length);
        builder.Insert(index, value);
    }

    public static void RemoveLinesContaining(
        this StringBuilder input,
        StringComparison comparison,
        params string[] stringToMatch
    )
    {
        if (stringToMatch.Length == 0)
        {
            throw new ArgumentException("At least one string to match is required", nameof(stringToMatch));
        }

        input.FilterLines(
            line =>
            {
                foreach (string toMatch in stringToMatch)
                {
                    if (line.Contains(toMatch, comparison))
                    {
                        return true;
                    }
                }

                return false;
            }
        );
    }

    private static void FilterLines(this StringBuilder input, Func<string, bool> removeLine)
    {
        input.ReplaceLines(line => removeLine(line) ? null : line);
    }

    public static void ReplaceLines(this StringBuilder input, Func<string, string?> replaceLine)
    {
        string text = input.ToString();
        using StringReader reader = new(text);
        input.Clear();
        while (reader.ReadLine() is { } line)
        {
            string? value = replaceLine(line);
            if (value is not null)
            {
                input.Append(value);
                input.Append('\n');
            }
        }

        if (input.Length > 0 && !text.EndsWith('\n'))
        {
            input.Length -= 1;
        }
    }
}
