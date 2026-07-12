# Design: Rename to SnapshotAssert

## Context

The library is currently named `SimpleVerify` everywhere: package ID, root namespace, assembly, three project folders, the solution file, MSBuild `buildTransitive` assets, CI/release workflows, the consumer test's NuGet source mapping, and the README. The name appears in ~147 places across ~50 tracked files. The NuGet ID `SimpleVerify` is taken by an unrelated commercial product, so the package cannot ship under this name. `SnapshotAssert` was verified unclaimed on nuget.org (2026-07-12).

Nothing has been published yet, so there are no external consumers and no compatibility constraints on the rename itself.

## Goals / Non-Goals

**Goals:**

- Every tracked file, project, and asset uses `SnapshotAssert` consistently; a repo-wide search for `SimpleVerify` (excluding `openspec/changes/archive/` and build output) finds nothing.
- Git history is preserved across file and folder renames.
- CI, the release pipeline, and the package-consumer integration test all pass under the new name.

**Non-Goals:**

- No behavior, API, or snapshot-format changes of any kind.
- No renaming of public types: `Verifier`, `VerifySettings`, `SettingsTask`, `VerifyException` keep their Verify-compatible names.
- Renaming the local working directory (`R:\private\SimpleVerify`) and the remote git repository — manual follow-ups outside the codebase.
- Publishing the package to claim the ID — done separately via the existing release workflow.

## Decisions

### Root namespace becomes `SnapshotAssert`, public type names stay

The namespace follows the package identity, so consumers see one coherent name. The type names are the Verify compatibility surface — `Verifier.Verify(...)` and `VerifySettings` are what make existing Verify-based test code compile unchanged — so they stay. Alternative considered: keeping the `SimpleVerify` namespace under the new package ID to shrink the diff; rejected because it permanently leaks a dead brand into every consumer's `global using`, and with zero published consumers the rename is free now and never will be again.

### Rename via `git mv` plus textual replacement, not tooling

Folders and files are renamed with `git mv` (preserves rename detection in history); content is updated with exact-string replacement of `SimpleVerify` → `SnapshotAssert`. The string is distinctive enough that blind replacement is safe within tracked files, with two exclusions: `openspec/changes/archive/**` (historical records stay as written) and untracked build output. Alternative considered: IDE-driven rename refactoring; rejected as it only covers namespaces, not csproj/workflow/config content, and this environment edits files directly.

### `buildTransitive` file names follow the PackageId

NuGet's build-integration convention imports `build/{PackageId}.props` and `buildTransitive/{PackageId}.targets` by exact name match. `SimpleVerify.props`/`.targets` are renamed to `SnapshotAssert.props`/`.targets`, and their contents (the injected `global using SnapshotAssert;` / `global using static SnapshotAssert.Verifier;`) updated together with the csproj `Pack` items that reference them. A name mismatch here fails silently — the package builds but consumers lose the global usings — which is why the consumer integration test is the acceptance gate.

### Main specs are updated through delta specs, not edited in place

`ci-validation`, `release-publishing`, and `xunit-v3-integration` name the package, csproj, or global usings at requirement level, so each gets a delta spec in this change; syncing/archiving folds them into `openspec/specs/`. The four snapshot-behavior specs (`snapshot-naming`, `snapshot-scrubbing`, `snapshot-serialization`, `snapshot-verification`) never mention the name and are untouched.

### Stale `SimplyVerify` build output is deleted

`tests/SimplyVerify.Tests/` and `tests/SimplyVerify.PackageConsumer/` contain only untracked `bin/` output from a pre-rename era. They are deleted rather than renamed — they are not part of the project.

## Risks / Trade-offs

- [Missed occurrence breaks something silently — e.g. a props file name mismatch drops global usings without failing the build] → Acceptance is behavioral, not textual: full solution build + unit tests + `dotnet pack` + PackageConsumer test run locally mirroring `ci.yml`, plus a final `git grep SimpleVerify` gate excluding the archive.
- [NuGet ID is first-come-first-served; `SnapshotAssert` could be claimed between rename and first publish] → Publish an initial version promptly after merging; the release workflow already exists.
- [`.verified.*` snapshot files could embed the old name and churn] → Checked: no snapshot file contains `SimpleVerify`; serialization output does not include library namespaces.
- [Untracked local state (`SimpleVerify.sln.DotSettings.user`, old `bin`/`obj`) keeps old paths] → Harmless; noted as local cleanup. Only `SimpleVerify.sln.DotSettings` is tracked and gets renamed with the solution file.

## Migration Plan

Single atomic commit on a branch: renames + content updates + green build. Rollback is reverting the commit. After merge, the user renames the GitHub repo and local folder, then tags a release to claim the NuGet ID.

## Open Questions

None.
