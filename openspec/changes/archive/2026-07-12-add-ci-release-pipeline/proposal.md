# Proposal: add-ci-release-pipeline

## Why

SimpleVerify has no automated validation or publishing: tests run only on developer machines and the NuGet package is produced by hand. Broken changes can land on master unnoticed, and releasing to nuget.org is a manual, error-prone ritual. GitHub Actions can validate every pull request and master push, and turn a version tag into a verified, published package.

## What Changes

- Add a CI workflow that builds the solution, runs the unit test suite, packs the NuGet package, and runs the package-consumer smoke test on every pull request and every push to master. It never publishes.
- Add a release workflow triggered by pushing a `v*` tag that repeats the full validation and then publishes the packed nupkg to nuget.org via Trusted Publishing (OIDC), guarded by a check that the tag version matches the csproj `<Version>`.
- Harden the package-consumer smoke test so it can never silently restore a published SimpleVerify package from nuget.org instead of the freshly packed one: float the `SimpleVerify` package reference and add package source mapping that pins `SimpleVerify` to the local artifacts feed.
- Document the release ritual: bump `<Version>` in a PR, merge, tag the merge commit `v<version>`, push the tag.

## Capabilities

### New Capabilities

- `ci-validation`: automated build, unit-test, pack, and package-consumer validation of pull requests and master pushes, including guaranteed resolution of the consumer's SimpleVerify reference from the locally packed artifact.
- `release-publishing`: tag-driven publication of the SimpleVerify package to nuget.org with full revalidation, tag/csproj version agreement enforcement, and keyless (OIDC) authentication.

### Modified Capabilities

None. Library runtime behavior is unchanged; existing specs (`snapshot-naming`, `snapshot-scrubbing`, `snapshot-serialization`, `snapshot-verification`, `xunit-v3-integration`) are unaffected.

## Impact

- New files: `.github/workflows/ci.yml`, `.github/workflows/release.yml`.
- Modified files: `tests/SimpleVerify.PackageConsumer/nuget.config` (package source mapping), `tests/SimpleVerify.PackageConsumer/SimpleVerify.PackageConsumer.csproj` (floating SimpleVerify version), `README.md` (release process documentation).
- External prerequisites: GitHub repository with a remote configured (none exists today), Trusted Publishing policy configured on nuget.org for this repository and workflow.
- No changes to library source or its package contents.
