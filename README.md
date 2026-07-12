# SimpleVerify

A minimal snapshot-testing library for xUnit v3, byte-for-byte compatible with the default text format of [Verify](https://github.com/VerifyTests/Verify).

SimpleVerify serializes objects with System.Text.Json contract metadata and compares the rendered output against `.verified.txt` files stored next to the test. On mismatch it writes a `.received.txt` file and launches your diff tool via
[DiffEngine](https://github.com/VerifyTests/DiffEngine).

## Scope

- xUnit v3 only (test identity via `TestContext.Current`).
- Supports the Verify API subset used by its consuming projects: `Verify(object)`,
  `Verify(object, VerifySettings)`, `VerifyJson(string)`, `UseParameters`, `UseFileName`, and the inventoried `VerifySettings` scrubbing/serialization methods.
- No Argon/Json.NET dependency.

## Requirements and limitations

- Snapshot files are located via the compile-time caller file path (`[CallerFilePath]`). Test projects must not enable `DeterministicSourcePaths` (implied by `ContinuousIntegrationBuild=true`), which rewrites source paths to `/_/...` and makes the snapshot directory unresolvable. SimpleVerify fails with a descriptive error when it detects this.

## Migrating a project from Verify.XunitV3

1. Replace the `Verify.XunitV3` package reference with `SimpleVerify`.
2. Remove `using Argon;` from settings builders. `NullValueHandling` and
   `DefaultValueHandling` keep their names, so `AddExtraSettings` lambda bodies compile unchanged.
3. Ensure `.gitattributes` contains `*.verified.txt text eol=lf` and re-checkout the snapshots if the working tree has CRLF endings. Verified files containing carriage returns fail fast, exactly as in Verify.
4. Build via the solution file so `$(SolutionDir)` is defined; the
   `{SolutionDirectory}` scrub token in snapshots requires it.
5. Run the test suite. Existing `.verified.txt` files must pass unchanged; any mismatch is a SimpleVerify bug, not a snapshot to re-approve.
