using System.Globalization;
using System.Text;
using SnapshotAssert.Scrubbing;
using SnapshotAssert.Serialization;

namespace SnapshotAssert;

public class VerifySettings
{
    public VerifySettings()
    {
    }

    internal VerifySettings(VerifySettings parent)
    {
        InstanceScrubbers.AddRange(parent.InstanceScrubbers);
        Serialization = parent.Serialization.Clone();
        ScrubGuidsSetting = parent.ScrubGuidsSetting;
        ScrubDateTimesSetting = parent.ScrubDateTimesSetting;
        CountDatesSetting = parent.CountDatesSetting;
        Parameters = parent.Parameters;
        FileName = parent.FileName;
    }

    internal List<Action<StringBuilder, Counter>> InstanceScrubbers { get; } = [];

    internal SerializationSettings Serialization { get; } = new();

    private bool? ScrubGuidsSetting { get; set; }

    private bool? ScrubDateTimesSetting { get; set; }

    private bool? CountDatesSetting { get; set; }

    internal object?[]? Parameters { get; private set; }

    internal string? FileName { get; private set; }

    public void UseParameters(params object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Length == 0)
        {
            throw new ArgumentException("At least one parameter value is required", nameof(parameters));
        }

        Parameters = parameters;
    }

    public void UseFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name must not be empty", nameof(fileName));
        }

        FileName = fileName;
    }

    internal bool EffectiveScrubGuids => ScrubGuidsSetting ?? true;

    internal bool EffectiveScrubDateTimes => ScrubDateTimesSetting ?? true;

    internal bool EffectiveCountDates => CountDatesSetting ?? true;

    public void DontScrubGuids()
    {
        ScrubGuidsSetting = false;
    }

    public void DontScrubDateTimes()
    {
        ScrubDateTimesSetting = false;
    }

    public void DisableDateCounting()
    {
        CountDatesSetting = false;
    }

    public void AddExtraSettings(Action<SerializationSettings> action)
    {
        action(Serialization);
    }

    public void DontIgnoreEmptyCollections()
    {
        Serialization.IgnoreEmptyCollections = false;
    }

    public void IgnoreMember(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Member name must not be empty", nameof(name));
        }

        Serialization.IgnoredMembers.Add(name);
    }

    public void AddScrubber(Action<StringBuilder> scrubber)
    {
        AddScrubber((builder, _) => scrubber(builder));
    }

    private void AddScrubber(Action<StringBuilder, Counter> scrubber)
    {
        InstanceScrubbers.Insert(0, scrubber);
    }

    public void ScrubLinesContaining(params string[] stringToMatch)
    {
        ScrubLinesContaining(StringComparison.OrdinalIgnoreCase, stringToMatch);
    }

    public void ScrubLinesContaining(StringComparison comparison, params string[] stringToMatch)
    {
        AddScrubber((builder, _) => builder.RemoveLinesContaining(comparison, stringToMatch));
    }

    public void ScrubLinesWithReplace(Func<string, string?> replaceLine)
    {
        AddScrubber((builder, _) => builder.ReplaceLines(replaceLine));
    }

    public void ScrubInlineGuids()
    {
        if (ScrubGuidsSetting == false)
        {
            throw new InvalidOperationException(
                "Guid scrubbing is disabled. Do not combine DontScrubGuids() with ScrubInlineGuids()."
            );
        }

        AddScrubber(GuidScrubber.ReplaceGuids);
    }

    public void ScrubInlineDateTimes(string format, CultureInfo? culture = null)
    {
        if (ScrubDateTimesSetting == false)
        {
            throw new InvalidOperationException(
                "Date scrubbing is disabled. Do not combine DontScrubDateTimes() with ScrubInlineDateTimes()."
            );
        }

        AddScrubber(DateScrubber.BuildDateTimeScrubber(format, culture));
    }
}
