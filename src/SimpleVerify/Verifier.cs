using System.Reflection;
using System.Runtime.CompilerServices;
using SimpleVerify.Engine;
using SimpleVerify.Naming;
using SimpleVerify.Scrubbing;
using Xunit;
using Xunit.v3;

namespace SimpleVerify;

public static class Verifier
{
    private static int _assemblyAssigned;

    public static SettingsTask Verify(
        object? target,
        VerifySettings? settings = null,
        [CallerFilePath] string sourceFile = ""
    )
    {
        return new SettingsTask(settings, resolved => BuildVerifier(resolved, sourceFile).Verify(target));
    }

    public static SettingsTask VerifyJson(
        string json,
        VerifySettings? settings = null,
        [CallerFilePath] string sourceFile = ""
    )
    {
        return new SettingsTask(settings, resolved => BuildVerifier(resolved, sourceFile).VerifyJson(json));
    }

    private static InnerVerifier BuildVerifier(VerifySettings settings, string sourceFile)
    {
        if (string.IsNullOrEmpty(sourceFile))
        {
            throw new VerifyException("Verify requires a caller file path to locate the snapshot directory");
        }

        if (TestContext.Current.TestMethod is not IXunitTestMethod testMethod)
        {
            throw new VerifyException(
                "Verify must be called from inside a running xUnit v3 test. TestContext.Current has no test method."
            );
        }

        MethodInfo method = testMethod.Method;
        Type type = method.ReflectedType ?? method.DeclaringType!;
        AssignTargetAssembly(type.Assembly);

        string typeName = GetTypeName(type);
        string[] parameterNames = method
            .GetParameters()
            .Select(_ => _.Name ?? string.Empty)
            .ToArray();
        string prefix = FileNameBuilder.Build(typeName, method.Name, parameterNames, settings);
        string directory = Path.GetDirectoryName(sourceFile)!;
        PrefixUnique.CheckPrefixIsUnique(Path.Combine(directory, prefix));
        return new InnerVerifier(directory, prefix, settings);
    }

    private static string GetTypeName(Type type)
    {
        string name = type.Name;
        int backtick = name.IndexOf('`');
        if (backtick != -1)
        {
            name = name[..backtick];
        }

        if (type.DeclaringType is not null)
        {
            return $"{GetTypeName(type.DeclaringType)}.{name}";
        }

        return name;
    }

    private static void AssignTargetAssembly(Assembly assembly)
    {
        if (Interlocked.Exchange(ref _assemblyAssigned, 1) == 1)
        {
            return;
        }

        string? solutionDirectory = null;
        string? projectDirectory = null;
        foreach (AssemblyMetadataAttribute attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            switch (attribute.Key)
            {
                case "SimpleVerify.SolutionDirectory":
                    solutionDirectory = attribute.Value;
                    break;
                case "SimpleVerify.ProjectDirectory":
                    projectDirectory = attribute.Value;
                    break;
            }
        }

        DirectoryReplacements.UseAssembly(solutionDirectory, projectDirectory);
    }
}
