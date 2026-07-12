using System.Globalization;
using System.Text;

namespace SimpleVerify.Writing;

internal class VerifyTextWriter(StringBuilder builder)
{
    private enum ScopeKind
    {
        Object,
        Array
    }

    private sealed class Scope(ScopeKind kind)
    {
        public ScopeKind Kind { get; } = kind;

        public int ChildCount { get; set; }
    }

    private readonly Stack<Scope> _scopes = new();
    private bool _pendingProperty;

    public void WriteStartObject()
    {
        BeginValue();
        builder.Append('{');
        _scopes.Push(new Scope(ScopeKind.Object));
    }

    public void WriteEndObject()
    {
        EndScope('}');
    }

    public void WriteStartArray()
    {
        BeginValue();
        builder.Append('[');
        _scopes.Push(new Scope(ScopeKind.Array));
    }

    public void WriteEndArray()
    {
        EndScope(']');
    }

    public void WritePropertyName(string name)
    {
        Scope scope = _scopes.Peek();
        if (scope.Kind != ScopeKind.Object)
        {
            throw new InvalidOperationException($"Cannot write property '{name}' outside an object scope");
        }

        if (scope.ChildCount > 0)
        {
            builder.Append(',');
        }

        AppendNewLineAndIndent(_scopes.Count);
        builder.Append(EscapeName(name));
        builder.Append(':');
        scope.ChildCount++;
        _pendingProperty = true;
    }

    private static string EscapeName(string name)
    {
        if (name.AsSpan().IndexOfAny('\n', '\r') == -1)
        {
            return name;
        }

        return name
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
    }

    public void WriteNull()
    {
        WriteRaw("null");
    }

    public void WriteRaw(string value)
    {
        BeginValue(value.Length == 0);
        builder.Append(value);
    }

    public void WriteString(string value)
    {
        if (_pendingProperty && value.Contains('\n'))
        {
            _pendingProperty = false;
            if (value[0] != '\n')
            {
                builder.Append('\n');
            }

            builder.Append(value);
            return;
        }

        WriteRaw(value);
    }

    public void WriteValue(bool value)
    {
        WriteRaw(value ? "true" : "false");
    }

    public void WriteValue(char value)
    {
        WriteRaw(value.ToString());
    }

    public void WriteValue(long value)
    {
        WriteRaw(value.ToString(CultureInfo.InvariantCulture));
    }

    public void WriteValue(ulong value)
    {
        WriteRaw(value.ToString(CultureInfo.InvariantCulture));
    }

    public void WriteValue(double value)
    {
        WriteRaw(EnsureDecimalPlace(value.ToString("R", CultureInfo.InvariantCulture)));
    }

    public void WriteValue(float value)
    {
        WriteRaw(EnsureDecimalPlace(value.ToString("R", CultureInfo.InvariantCulture)));
    }

    public void WriteValue(decimal value)
    {
        WriteRaw(EnsureDecimalPlace(value.ToString(CultureInfo.InvariantCulture)));
    }

    public void WriteValue(byte[] value)
    {
        WriteRaw(Convert.ToBase64String(value));
    }

    public void WriteValue(TimeSpan value)
    {
        WriteRaw(value.ToString());
    }

    private void BeginValue(bool isEmptyValue = false)
    {
        if (_pendingProperty)
        {
            _pendingProperty = false;
            if (!isEmptyValue)
            {
                builder.Append(' ');
            }

            return;
        }

        if (_scopes.Count == 0)
        {
            return;
        }

        Scope scope = _scopes.Peek();
        if (scope.Kind != ScopeKind.Array)
        {
            throw new InvalidOperationException(
                "Cannot write a value directly inside an object scope without a property name"
            );
        }

        if (scope.ChildCount > 0)
        {
            builder.Append(',');
        }

        if (isEmptyValue)
        {
            builder.Append('\n');
        }
        else
        {
            AppendNewLineAndIndent(_scopes.Count);
        }

        scope.ChildCount++;
    }

    private void EndScope(char close)
    {
        Scope scope = _scopes.Pop();
        if (scope.ChildCount > 0)
        {
            AppendNewLineAndIndent(_scopes.Count);
        }

        builder.Append(close);
    }

    private void AppendNewLineAndIndent(int depth)
    {
        builder.Append('\n');
        builder.Append(' ', depth * 2);
    }

    private static string EnsureDecimalPlace(string text)
    {
        if (text.AsSpan().IndexOfAny('.', 'E', 'e') != -1 || text is "NaN" or "Infinity" or "-Infinity")
        {
            return text;
        }

        return $"{text}.0";
    }

    public override string ToString()
    {
        return builder.ToString();
    }
}
