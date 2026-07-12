# Rename to SnapshotAssert

## Why

The `SimpleVerify` package ID is already taken on nuget.org by an unrelated, actively maintained commercial product (the simpleverify.io verification-alerts client), so the library cannot be published under its current name. `SnapshotAssert` is unclaimed on nuget.org, describes what the library does without leaning on the Verify project's brand, and cannot be confused with the existing package.

## What Changes

- **BREAKING**: The NuGet package ID changes from `SimpleVerify` to `SnapshotAssert`.
- **BREAKING**: The root namespace changes from `SimpleVerify` to `SnapshotAssert`; the `buildTransitive` props inject `global using SnapshotAssert;` and `global using static SnapshotAssert.Verifier;` instead of the `SimpleVerify` equivalents.
- Project folders, `.csproj` files, the solution file, `InternalsVisibleTo`, the `buildTransitive` props/targets file names, CI/release workflows, the consumer test's NuGet mapping, and the README all adopt the new name.
- Public type names stay unchanged (`Verifier`, `VerifySettings`, `SettingsTask`, `VerifyException`) — Verify API compatibility is a feature, not part of the branding.
- No behavior changes: snapshot format, naming, scrubbing, and serialization are untouched.

## Capabilities

### New Capabilities

None — this change renames existing capabilities without introducing new behavior.

### Modified Capabilities

- `ci-validation`: Requirements name the packed package, test project, and consumer package mapping as `SimpleVerify`; these become `SnapshotAssert`.
- `release-publishing`: The version-match requirement references `SimpleVerify.csproj`; it becomes `SnapshotAssert.csproj`.
- `xunit-v3-integration`: The `buildTransitive` requirement mandates injecting `global using SimpleVerify;` and `global using static SimpleVerify.Verifier;`; these become the `SnapshotAssert` equivalents.

## Impact

- **Source**: `src/SimpleVerify/` → `src/SnapshotAssert/` (folder, csproj, `PackageId`, namespaces in all source files, `buildTransitive/SnapshotAssert.props/.targets` — file names must match the new `PackageId` for NuGet build integration to work).
- **Tests**: `tests/SimpleVerify.Tests/` and `tests/SimpleVerify.PackageConsumer/` folders, csproj files, namespaces, and the consumer's `nuget.config` package-ID mapping.
- **Solution**: `SimpleVerify.slnx` and ReSharper `.DotSettings` files renamed; project paths inside updated.
- **CI/CD**: `.github/workflows/ci.yml` and `release.yml` references to project paths and package name.
- **Docs**: `README.md` title, install instructions, and package references.
- **Out of scope**: renaming the local working directory and the remote git repository (manual follow-ups); archived OpenSpec changes stay untouched as historical records.
- No published consumers exist (the package was never on nuget.org), so the breaking changes affect no one.
