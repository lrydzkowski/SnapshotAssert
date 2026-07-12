# xunit-v3-integration Specification

## Purpose

Define how the library integrates with xUnit v3 (test identity via `TestContext`, source-file-relative snapshot placement) and what the NuGet package ships (implicit usings, directory metadata, minimal dependencies).

## Requirements

### Requirement: Test identity from TestContext

The library SHALL resolve the current test's class name, method name, and method parameter metadata from xUnit v3's `TestContext.Current` at verification time, requiring no attribute or base class on test classes. When no test context is active, the verification SHALL fail with a descriptive error stating that verifications must run inside an xUnit v3 test.

#### Scenario: Identity resolved inside a test

- **WHEN** a verification is awaited inside a running xUnit v3 test
- **THEN** the snapshot prefix derives from that test's class and method names without any attribute on the test class

#### Scenario: Called outside a test

- **WHEN** a verification is awaited where `TestContext.Current` has no active test
- **THEN** it fails with an error explaining the xUnit v3 test-context requirement

### Requirement: Source-file-relative snapshot directory

The library SHALL capture the calling test's source file path via `[CallerFilePath]` on the `Verify`/`VerifyJson` entry points and use its directory as the snapshot directory, matching Verify's placement of snapshots next to the test file. When the resolved snapshot directory does not exist on disk, the verification SHALL fail with a descriptive `VerifyException` before any file operation; when the captured source file path shows PathMap rewriting (a leading `/_`), the error message SHALL name `DeterministicSourcePaths`/`ContinuousIntegrationBuild` as the likely cause and state that they must be disabled for test projects.

#### Scenario: Snapshot lands next to the test source

- **WHEN** a test in `GetAuditLogsTests.cs` awaits a verification
- **THEN** received and verified files are read and written in the directory containing `GetAuditLogsTests.cs`

#### Scenario: PathMap-rewritten caller file path

- **WHEN** a verification runs in a build where `[CallerFilePath]` was rewritten to a `/_/...` path by `DeterministicSourcePaths`
- **THEN** it fails with a `VerifyException` explaining that deterministic source paths must be disabled for test projects, instead of a low-level file-system error

### Requirement: Theory parameter names available to naming

The library SHALL obtain the test method's parameter names from the xUnit v3 test metadata so `UseParameters` can pair supplied values with parameter names in declaration order.

#### Scenario: Parameter names paired

- **WHEN** a theory method declares parameter `testCase` and the test calls `UseParameters(value)`
- **THEN** the file-name segment uses the name `testCase`

### Requirement: Package injects implicit usings

The NuGet package SHALL ship `buildTransitive` MSBuild props that, when the consuming project has `ImplicitUsings` enabled, inject `global using SimpleVerify;` and `global using static SimpleVerify.Verifier;` so existing TMHE test code calling bare `Verify(...)` and using `VerifySettings` compiles without source changes.

#### Scenario: Bare Verify call compiles

- **WHEN** a test project with `ImplicitUsings` enabled references the SimpleVerify package
- **THEN** `await Verify(result);` and `new VerifySettings()` compile without any `using` directives in the test file

### Requirement: Package embeds directory metadata for path scrubbing

The NuGet package SHALL ship `buildTransitive` MSBuild targets that embed the consuming test project's solution and project directory paths as assembly metadata at build time, providing the values the always-on directory-replacement scrubber resolves at run time.

#### Scenario: Metadata available at run time

- **WHEN** a test project referencing the package is built and its tests run
- **THEN** the library resolves the project's solution and project directory paths from the test assembly's metadata

### Requirement: Minimal dependency footprint

The package SHALL target `net10.0` and depend only on `xunit.v3.extensibility.core` and `DiffEngine`. It SHALL NOT depend on Argon, Newtonsoft.Json, or any Json.NET fork; serialization SHALL use System.Text.Json from the base class library.

#### Scenario: Dependency graph inspected

- **WHEN** the package's dependency closure is resolved
- **THEN** it contains DiffEngine and xunit.v3.extensibility.core and no Json.NET-family package
