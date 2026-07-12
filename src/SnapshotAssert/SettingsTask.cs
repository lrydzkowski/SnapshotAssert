using System.Runtime.CompilerServices;

namespace SnapshotAssert;

public class SettingsTask
{
    private readonly VerifySettings _settings;
    private readonly Func<VerifySettings, Task> _buildTask;
    private Task? _task;

    internal SettingsTask(VerifySettings? settings, Func<VerifySettings, Task> buildTask)
    {
        _settings = settings is null ? new VerifySettings() : new VerifySettings(settings);
        _buildTask = buildTask;
    }

    public SettingsTask UseParameters(params object?[] parameters)
    {
        _settings.UseParameters(parameters);

        return this;
    }

    public SettingsTask UseFileName(string fileName)
    {
        _settings.UseFileName(fileName);

        return this;
    }

    private Task ToTask()
    {
        return _task ??= _buildTask(_settings);
    }

    public TaskAwaiter GetAwaiter()
    {
        return ToTask().GetAwaiter();
    }

    public static implicit operator Task(SettingsTask settingsTask)
    {
        return settingsTask.ToTask();
    }
}
