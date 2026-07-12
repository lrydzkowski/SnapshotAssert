# SnapshotAssert

A minimal snapshot-testing library for xUnit v3, byte-for-byte compatible with the default text format of [Verify](https://github.com/VerifyTests/Verify).

SnapshotAssert serializes objects with System.Text.Json contract metadata and compares the rendered output against `.verified.txt` files stored next to the test. On mismatch it writes a `.received.txt` file and launches your diff tool via
[DiffEngine](https://github.com/VerifyTests/DiffEngine).

## Scope

- xUnit v3 only (test identity via `TestContext.Current`).
- Supports the Verify API subset used by its consuming projects: `Verify(object)`,
  `Verify(object, VerifySettings)`, `VerifyJson(string)`, `UseParameters`, `UseFileName`, and the inventoried `VerifySettings` scrubbing/serialization methods.
- No Argon/Json.NET dependency.

## Requirements and limitations

- Snapshot files are located via the compile-time caller file path (`[CallerFilePath]`). Test projects must not enable `DeterministicSourcePaths` (implied by `ContinuousIntegrationBuild=true`), which rewrites source paths to `/_/...` and makes the snapshot directory unresolvable. SnapshotAssert fails with a descriptive error when it detects this.

## Releasing

1. Open a pull request that bumps `<Version>` in `src/SnapshotAssert/SnapshotAssert.csproj` and merge it.
2. Tag the merge commit and push the tag:

   ```bash
   git tag v<version>
   git push origin v<version>
   ```

3. The `release.yml` workflow revalidates the tagged commit (build, unit tests, pack, package-consumer tests) and publishes the package to nuget.org.

The tag must match the csproj version exactly (`v0.2.0` requires `<Version>0.2.0</Version>`); a mismatch fails the release before any publish. Publishing a version that already exists on nuget.org also fails — bump the version instead of re-tagging.

### Local packaging note

`tests/SnapshotAssert.PackageConsumer` restores SnapshotAssert exclusively from the local `artifacts/` feed (`dotnet pack src/SnapshotAssert/SnapshotAssert.csproj -o artifacts`). When repacking the same version locally, NuGet may serve a stale copy from the global cache; clear it with `dotnet nuget locals global-packages --clear`.

## Migrating a project from Verify.XunitV3

1. Replace the `Verify.XunitV3` package reference with `SnapshotAssert`.
2. Remove `using Argon;` from settings builders. `NullValueHandling` and
   `DefaultValueHandling` keep their names, so `AddExtraSettings` lambda bodies compile unchanged.
3. Ensure `.gitattributes` contains `*.verified.txt text eol=lf` and re-checkout the snapshots if the working tree has CRLF endings. Verified files containing carriage returns fail fast, exactly as in Verify.
4. Build via the solution file so `$(SolutionDir)` is defined; the
   `{SolutionDirectory}` scrub token in snapshots requires it.
5. Run the test suite. Existing `.verified.txt` files must pass unchanged; any mismatch is a SnapshotAssert bug, not a snapshot to re-approve.
