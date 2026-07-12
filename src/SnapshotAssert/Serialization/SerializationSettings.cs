namespace SnapshotAssert.Serialization;

public class SerializationSettings
{
    public NullValueHandling NullValueHandling { get; set; } = NullValueHandling.Ignore;

    public DefaultValueHandling DefaultValueHandling { get; set; } = DefaultValueHandling.Ignore;

    internal bool IgnoreEmptyCollections { get; set; } = true;

    internal HashSet<string> IgnoredMembers { get; private init; } = [];

    internal SerializationSettings Clone()
    {
        return new SerializationSettings
        {
            NullValueHandling = NullValueHandling,
            DefaultValueHandling = DefaultValueHandling,
            IgnoreEmptyCollections = IgnoreEmptyCollections,
            IgnoredMembers = [.. IgnoredMembers]
        };
    }
}

public enum NullValueHandling
{
    Include = 0,
    Ignore = 1
}

public enum DefaultValueHandling
{
    Include = 0,
    Ignore = 1
}
