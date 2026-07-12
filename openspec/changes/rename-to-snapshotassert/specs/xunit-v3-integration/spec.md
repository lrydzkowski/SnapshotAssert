# xunit-v3-integration Delta

## MODIFIED Requirements

### Requirement: Package injects implicit usings

The NuGet package SHALL ship `buildTransitive` MSBuild props that, when the consuming project has `ImplicitUsings` enabled, inject `global using SnapshotAssert;` and `global using static SnapshotAssert.Verifier;` so existing TMHE test code calling bare `Verify(...)` and using `VerifySettings` compiles without source changes.

#### Scenario: Bare Verify call compiles

- **WHEN** a test project with `ImplicitUsings` enabled references the SnapshotAssert package
- **THEN** `await Verify(result);` and `new VerifySettings()` compile without any `using` directives in the test file
