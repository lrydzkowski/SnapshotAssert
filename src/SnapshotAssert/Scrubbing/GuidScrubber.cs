using System.Text;

namespace SnapshotAssert.Scrubbing;

internal static class GuidScrubber
{
    private const int GuidLength = 36;

    public static void ReplaceGuids(StringBuilder builder, Counter counter)
    {
        if (!counter.ScrubGuids)
        {
            return;
        }

        if (builder.Length < GuidLength)
        {
            return;
        }

        string text = builder.ToString();
        ReadOnlySpan<char> span = text.AsSpan();
        List<(int Index, string Value)> matches = new();

        for (int index = 0; index <= span.Length - GuidLength; index++)
        {
            if (index != 0 && IsInvalidStartingChar(span[index - 1]))
            {
                continue;
            }

            int end = index + GuidLength;
            if (end != span.Length && IsInvalidEndingChar(span[end]))
            {
                continue;
            }

            if (!Guid.TryParseExact(span.Slice(index, GuidLength), "D", out Guid guid))
            {
                continue;
            }

            matches.Add((index, counter.Convert(guid)));
            index += GuidLength - 1;
        }

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            builder.Overwrite(matches[i].Value, matches[i].Index, GuidLength);
        }
    }

    private static bool IsInvalidChar(char ch)
    {
        return char.IsLetter(ch) || char.IsNumber(ch);
    }

    private static bool IsInvalidStartingChar(char ch)
    {
        return IsInvalidChar(ch) && ch != '{' && ch != '(';
    }

    private static bool IsInvalidEndingChar(char ch)
    {
        return IsInvalidChar(ch) && ch != '}' && ch != ')';
    }
}
