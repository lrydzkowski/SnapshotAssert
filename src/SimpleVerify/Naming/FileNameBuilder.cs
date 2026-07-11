using System.Globalization;
using SimpleVerify.Engine;

namespace SimpleVerify.Naming;

internal static class FileNameBuilder
{
    public static string Build(
        string typeName,
        string methodName,
        IReadOnlyList<string> parameterNames,
        VerifySettings settings
    )
    {
        if (settings.FileName is not null)
        {
            return settings.FileName;
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
        return $"{prefix}_{string.Join("_", segments)}";
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
