using System.Globalization;
using System.Text;
using SnapshotAssert.Engine;

namespace SnapshotAssert.Naming;

internal static class FileNameBuilder
{
    private static readonly HashSet<char> InvalidCharacters = BuildInvalidCharacters();

    public static string Build(
        string typeName,
        string methodName,
        IReadOnlyList<string> parameterNames,
        VerifySettings settings
    )
    {
        if (settings.FileName is not null)
        {
            return Sanitize(settings.FileName);
        }

        string prefix = $"{typeName}.{methodName}";
        object?[]? parameters = settings.Parameters;
        if (parameters is null)
        {
            if (parameterNames.Count > 0)
            {
                throw new VerifyException(
                    $"The test method '{methodName}' has parameters. Call UseParameters(...) or UseFileName(...) to make the snapshot file name unique per test case."
                );
            }

            return prefix;
        }

        if (parameters.Length > parameterNames.Count)
        {
            throw new VerifyException(
                $"UseParameters received {parameters.Length} values but the test method '{methodName}' has only {parameterNames.Count} parameters."
            );
        }

        string[] segments = parameters
            .Select((value, index) => $"{parameterNames[index]}={Format(value)}")
            .ToArray();
        return Sanitize($"{prefix}_{string.Join("_", segments)}");
    }

    private static HashSet<char> BuildInvalidCharacters()
    {
        HashSet<char> characters = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];
        for (char character = '\0'; character < ' '; character++)
        {
            characters.Add(character);
        }

        foreach (char character in Path.GetInvalidFileNameChars())
        {
            characters.Add(character);
        }

        return characters;
    }

    private static string Sanitize(string prefix)
    {
        StringBuilder builder = new(prefix.Length);
        foreach (char character in prefix)
        {
            builder.Append(InvalidCharacters.Contains(character) ? '-' : character);
        }

        return builder.ToString();
    }

    private static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            string stringValue => stringValue,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null"
        };
    }
}
