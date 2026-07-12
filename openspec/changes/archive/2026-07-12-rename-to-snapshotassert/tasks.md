# Tasks: Rename to SnapshotAssert

## 1. Rename files and folders

- [x] 1.1 `git mv src/SimpleVerify src/SnapshotAssert` and `git mv src/SnapshotAssert/SimpleVerify.csproj src/SnapshotAssert/SnapshotAssert.csproj`
- [x] 1.2 `git mv` the `buildTransitive` assets: `SimpleVerify.props` → `SnapshotAssert.props`, `SimpleVerify.targets` → `SnapshotAssert.targets`
- [x] 1.3 `git mv tests/SimpleVerify.Tests tests/SnapshotAssert.Tests` and rename its csproj to `SnapshotAssert.Tests.csproj`
- [x] 1.4 `git mv tests/SimpleVerify.PackageConsumer tests/SnapshotAssert.PackageConsumer` and rename its csproj to `SnapshotAssert.PackageConsumer.csproj`
- [x] 1.5 `git mv SimpleVerify.slnx SnapshotAssert.slnx` and `git mv SimpleVerify.sln.DotSettings SnapshotAssert.sln.DotSettings`
- [x] 1.6 Delete the stale untracked `tests/SimplyVerify.Tests/` and `tests/SimplyVerify.PackageConsumer/` build-output folders, plus old `bin`/`obj` under the renamed projects

## 2. Update content

- [x] 2.1 Replace `SimpleVerify` with `SnapshotAssert` in `src/SnapshotAssert/SnapshotAssert.csproj` (`PackageId`, `InternalsVisibleTo`, `buildTransitive` Pack items) and in the props/targets file contents (global usings, MSBuild property names)
- [x] 2.2 Update the namespace in all `.cs` files under `src/SnapshotAssert/` from `SimpleVerify` to `SnapshotAssert`
- [x] 2.3 Update namespaces and `using` directives in all `.cs` files under `tests/SnapshotAssert.Tests/` and `tests/SnapshotAssert.PackageConsumer/`
- [x] 2.4 Update `tests/SnapshotAssert.PackageConsumer/SnapshotAssert.PackageConsumer.csproj` package reference and `nuget.config` package-source-mapping pattern to `SnapshotAssert`
- [x] 2.5 Update project paths and names in `SnapshotAssert.slnx`
- [x] 2.6 Update `.github/workflows/ci.yml` and `.github/workflows/release.yml` (solution name, csproj paths, consumer test path)
- [x] 2.7 Update `README.md` (title, install instructions, package name references)

## 3. Verify

- [x] 3.1 `git grep -l SimpleVerify` returns only files under `openspec/changes/archive/` (historical records) and `openspec/specs/` (updated via delta sync at archive time) — no tracked source, config, workflow, or doc hits remain
- [x] 3.2 `dotnet build SnapshotAssert.slnx --configuration Release` succeeds with zero warnings (warnings are errors)
- [x] 3.3 `dotnet test SnapshotAssert.slnx --configuration Release --no-build` passes
- [x] 3.4 `dotnet pack src/SnapshotAssert/SnapshotAssert.csproj --configuration Release --no-build -o artifacts` produces `SnapshotAssert.<version>.nupkg` containing `buildTransitive/SnapshotAssert.props` and `.targets`
- [x] 3.5 `dotnet test tests/SnapshotAssert.PackageConsumer/SnapshotAssert.PackageConsumer.csproj --configuration Release` passes, proving the packed package restores and the injected global usings work
