using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using SnapshotAssert.Engine;
using SnapshotAssert.Scrubbing;
using SnapshotAssert.Writing;

namespace SnapshotAssert.Serialization;

internal class ObjectWalker(VerifySettings settings, Counter counter, VerifyTextWriter writer)
{
    private sealed record MemberEntry(string Name, Type MemberType, Func<object, object?> Get);

    private static readonly DefaultJsonTypeInfoResolver Resolver = new();

    private static readonly JsonSerializerOptions TypeInfoOptions = new()
    {
        IncludeFields = true
    };

    private static readonly ConcurrentDictionary<Type, MemberEntry[]?> MemberCache = new();
    private static readonly ConcurrentDictionary<Type, object?> DefaultValueCache = new();

    private readonly List<object> _ancestors = [];

    public void WriteTarget(object? value)
    {
        WriteValue(value);
    }

    private void WriteValue(object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNull();
                return;
            case string stringValue:
                WriteString(stringValue);
                return;
            case char charValue:
                writer.WriteValue(charValue);
                return;
            case bool boolValue:
                writer.WriteValue(boolValue);
                return;
            case Guid guidValue:
                WriteGuid(guidValue);
                return;
            case DateTime dateTime:
                WriteDateTime(dateTime);
                return;
            case DateTimeOffset dateTimeOffset:
                WriteDateTimeOffset(dateTimeOffset);
                return;
            case TimeSpan timeSpan:
                writer.WriteValue(timeSpan);
                return;
            case byte[] bytes:
                writer.WriteValue(bytes);
                return;
            case sbyte or byte or short or ushort or int or uint or long:
                writer.WriteValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                return;
            case ulong ulongValue:
                writer.WriteValue(ulongValue);
                return;
            case float floatValue:
                writer.WriteValue(floatValue);
                return;
            case double doubleValue:
                writer.WriteValue(doubleValue);
                return;
            case decimal decimalValue:
                writer.WriteValue(decimalValue);
                return;
            case Enum enumValue:
                WriteString(enumValue.ToString());
                return;
            case DateOnly dateOnly:
                WriteDateTime(dateOnly.ToDateTime(TimeOnly.MinValue));
                return;
            case TimeOnly timeOnly:
                writer.WriteRaw(timeOnly.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture));
                return;
            case Uri uri:
                WriteString(uri.OriginalString);
                return;
            case Version version:
                WriteString(version.ToString());
                return;
            case JsonNode node:
                WriteJsonNode(node);
                return;
        }

        if (TryGetDictionaryEntries(value, out List<(object Key, object? Value)> entries))
        {
            WriteDictionary(value, entries);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            WriteArray(value, enumerable);
            return;
        }

        WriteObject(value);
    }

    private void WriteString(string value)
    {
        if (value.Length == 0)
        {
            writer.WriteRaw(string.Empty);
            return;
        }

        if (counter.TryConvert(value.AsSpan(), out string? converted))
        {
            writer.WriteRaw(converted!);
            return;
        }

        writer.WriteString(NormalizeNewlines(value));
    }

    private static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private void WriteGuid(Guid value)
    {
        if (counter.TryConvert(value, out string? converted))
        {
            writer.WriteRaw(converted!);
            return;
        }

        writer.WriteRaw(value.ToString("D"));
    }

    private void WriteDateTime(DateTime value)
    {
        if (counter.TryConvert(value, out string? converted))
        {
            writer.WriteRaw(converted!);
            return;
        }

        writer.WriteRaw(DateFormatter.Convert(value));
    }

    private void WriteDateTimeOffset(DateTimeOffset value)
    {
        if (counter.TryConvert(value, out string? converted))
        {
            writer.WriteRaw(converted!);
            return;
        }

        writer.WriteRaw(DateFormatter.Convert(value));
    }

    private void WriteObject(object value)
    {
        MemberEntry[]? members = GetMembers(value.GetType());
        if (members is null)
        {
            throw new VerifyException(
                $"Type '{value.GetType()}' is serialized by a built-in converter and has no members to render. "
                + "SnapshotAssert has no rendering for it; convert the value to a supported type before verifying."
            );
        }

        writer.WriteStartObject();
        Push(value);
        foreach (MemberEntry member in members)
        {
            object? memberValue;
            try
            {
                memberValue = member.Get(value);
            }
            catch (Exception exception) when (exception is NotImplementedException or NotSupportedException)
            {
                continue;
            }

            WriteMember(value, memberValue, member.Name, member.MemberType);
        }

        Pop(value);
        writer.WriteEndObject();
    }

    private void WriteMember(object target, object? value, string name, Type memberType)
    {
        if (settings.Serialization.IgnoredMembers.Contains(name))
        {
            return;
        }

        if (typeof(Stream).IsAssignableFrom(memberType))
        {
            return;
        }

        if (value is null)
        {
            WriteNullMember(name);
            return;
        }

        if (ReferenceEquals(target, value))
        {
            writer.WritePropertyName(name);
            writer.WriteRaw("$parentValue");
            return;
        }

        if (IsOnStack(value))
        {
            return;
        }

        if (value is IEnumerable lazyEnumerable
            && value is not (string or byte[] or ICollection)
            && !IsDictionaryShaped(value))
        {
            value = lazyEnumerable.Cast<object?>().ToList();
        }

        if (settings.Serialization.IgnoreEmptyCollections
            && value is not (string or byte[])
            && value is IEnumerable collection
            && IsEmpty(collection))
        {
            WriteNullMember(name);
            return;
        }

        if (settings.Serialization.DefaultValueHandling == DefaultValueHandling.Ignore
            && memberType != typeof(bool)
            && memberType.IsValueType
            && Nullable.GetUnderlyingType(memberType) is null
            && Equals(value, GetDefaultValue(memberType)))
        {
            return;
        }

        writer.WritePropertyName(name);
        WriteValue(value);
    }

    private void WriteNullMember(string name)
    {
        if (settings.Serialization.NullValueHandling == NullValueHandling.Include)
        {
            writer.WritePropertyName(name);
            writer.WriteNull();
        }
    }

    private void WriteDictionary(object dictionary, List<(object Key, object? Value)> entries)
    {
        writer.WriteStartObject();
        Push(dictionary);
        foreach ((string key, object? value) in entries
                     .Select(entry => (Key: ConvertDictionaryKey(entry.Key), entry.Value))
                     .OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            if (value is null)
            {
                WriteNullMember(key);
                continue;
            }

            if (IsOnStack(value))
            {
                continue;
            }

            writer.WritePropertyName(key);
            WriteValue(value);
        }

        Pop(dictionary);
        writer.WriteEndObject();
    }

    private string ConvertDictionaryKey(object key)
    {
        return key switch
        {
            string stringKey => stringKey,
            Guid guid => counter.TryConvert(guid, out string? converted) ? converted! : guid.ToString("D"),
            DateTime dateTime => counter.TryConvert(dateTime, out string? converted)
                ? converted!
                : DateFormatter.Convert(dateTime),
            DateTimeOffset dateTimeOffset => counter.TryConvert(dateTimeOffset, out string? converted)
                ? converted!
                : DateFormatter.Convert(dateTimeOffset),
            DateOnly dateOnly => ConvertDictionaryKey(dateOnly.ToDateTime(TimeOnly.MinValue)),
            TimeOnly timeOnly => timeOnly.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
            _ => Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private void WriteArray(object value, IEnumerable enumerable)
    {
        writer.WriteStartArray();
        Push(value);
        foreach (object? item in enumerable)
        {
            if (item is not null && IsOnStack(item))
            {
                continue;
            }

            WriteValue(item);
        }

        Pop(value);
        writer.WriteEndArray();
    }

    private void WriteJsonNode(JsonNode? node)
    {
        switch (node)
        {
            case null:
                writer.WriteNull();
                return;
            case JsonObject jsonObject:
                writer.WriteStartObject();
                foreach (KeyValuePair<string, JsonNode?> pair in jsonObject)
                {
                    writer.WritePropertyName(pair.Key);
                    WriteJsonNode(pair.Value);
                }

                writer.WriteEndObject();
                return;
            case JsonArray jsonArray:
                writer.WriteStartArray();
                foreach (JsonNode? item in jsonArray)
                {
                    WriteJsonNode(item);
                }

                writer.WriteEndArray();
                return;
        }

        JsonValue jsonValue = (JsonValue)node;
        if (jsonValue.TryGetValue<JsonElement>(out JsonElement element))
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    WriteString(element.GetString()!);
                    return;
                case JsonValueKind.Number:
                    writer.WriteRaw(element.GetRawText());
                    return;
                case JsonValueKind.True:
                    writer.WriteValue(true);
                    return;
                case JsonValueKind.False:
                    writer.WriteValue(false);
                    return;
                case JsonValueKind.Null:
                    writer.WriteNull();
                    return;
            }
        }

        WriteString(jsonValue.ToString());
    }

    private void Push(object value)
    {
        if (_ancestors.Count >= 100)
        {
            throw new InvalidOperationException("Object graph nesting exceeds 100 levels; possible undetected cycle");
        }

        if (!value.GetType().IsValueType)
        {
            _ancestors.Add(value);
        }
    }

    private void Pop(object value)
    {
        if (!value.GetType().IsValueType)
        {
            _ancestors.RemoveAt(_ancestors.Count - 1);
        }
    }

    private bool IsOnStack(object value)
    {
        if (value.GetType().IsValueType || value is string)
        {
            return false;
        }

        foreach (object ancestor in _ancestors)
        {
            if (ReferenceEquals(ancestor, value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDictionaryShaped(object value)
    {
        return value is IDictionary
               || value
                   .GetType()
                   .GetInterfaces()
                   .Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
    }

    private static bool IsEmpty(IEnumerable enumerable)
    {
        if (enumerable is ICollection collection)
        {
            return collection.Count == 0;
        }

        IEnumerator enumerator = enumerable.GetEnumerator();
        try
        {
            return !enumerator.MoveNext();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    private static bool TryGetDictionaryEntries(object value, out List<(object Key, object? Value)> entries)
    {
        if (value is IDictionary dictionary)
        {
            entries = [];
            foreach (DictionaryEntry entry in dictionary)
            {
                entries.Add((entry.Key, entry.Value));
            }

            return true;
        }

        Type? readOnlyInterface = value
            .GetType()
            .GetInterfaces()
            .FirstOrDefault(
                _ =>
                    _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)
            );
        if (readOnlyInterface is null)
        {
            entries = [];
            return false;
        }

        entries = [];
        foreach (object? item in (IEnumerable)value)
        {
            Type itemType = item!.GetType();
            object key = itemType.GetProperty("Key")!.GetValue(item)!;
            object? itemValue = itemType.GetProperty("Value")!.GetValue(item);
            entries.Add((key, itemValue));
        }

        return true;
    }

    private static object? GetDefaultValue(Type type)
    {
        return DefaultValueCache.GetOrAdd(type, static _ => Activator.CreateInstance(_));
    }

    private static MemberEntry[]? GetMembers(Type type)
    {
        return MemberCache.GetOrAdd(type, static _ => BuildMembers(_));
    }

    private static MemberEntry[]? BuildMembers(Type type)
    {
        JsonTypeInfo typeInfo = Resolver.GetTypeInfo(type, TypeInfoOptions);
        if (typeInfo.Kind == JsonTypeInfoKind.None)
        {
            return type == typeof(object) ? [] : null;
        }

        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return [];
        }

        return typeInfo.Properties
            .Where(_ => _.Get is not null && _.AttributeProvider is MemberInfo)
            .Select(_ => (Property: _, Member: (MemberInfo)_.AttributeProvider!))
            .OrderBy(_ => GetInheritanceDepth(_.Member.DeclaringType!))
            .ThenBy(_ => _.Member is FieldInfo)
            .ThenBy(_ => _.Member.MetadataToken)
            .Select(_ => new MemberEntry(_.Property.Name, _.Property.PropertyType, _.Property.Get!))
            .ToArray();
    }

    private static int GetInheritanceDepth(Type type)
    {
        int depth = 0;
        Type? current = type;
        while (current.BaseType is not null)
        {
            depth++;
            current = current.BaseType;
        }

        return depth;
    }
}
