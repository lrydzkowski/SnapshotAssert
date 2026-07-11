using System.Runtime.CompilerServices;
using DiffEngine;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SimpleVerify.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        DiffRunner.Disabled = true;
    }
}
