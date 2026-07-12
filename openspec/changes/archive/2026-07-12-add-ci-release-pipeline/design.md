# Design: add-ci-release-pipeline

## Context

SimpleVerify is a single-library solution (`src/SimpleVerify`) with two test projects: `SimpleVerify.Tests` (unit tests via project reference) and `SimpleVerify.PackageConsumer` (smoke tests that restore the packed nupkg from a local `artifacts/` feed declared in its `nuget.config`). The package version is hardcoded as `<Version>` in `SimpleVerify.csproj`, and the consumer currently pins the same version in its `PackageReference`. There is no CI, no publishing automation, and no GitHub remote yet. The `artifacts/` directory is gitignored, so a fresh checkout starts with an empty local feed.

Two constraints come from the library itself: snapshot resolution relies on `[CallerFilePath]`, so test projects must never build with `ContinuousIntegrationBuild=true` (it rewrites source paths and breaks snapshot lookup), and `.verified.txt` files are LF-normalized via `.gitattributes`, which keeps byte-for-byte comparisons stable on Linux runners.

## Goals / Non-Goals

**Goals:**

- Every pull request and every push to master is validated: build, unit tests, pack, consumer smoke test.
- Pushing a `v*` tag produces a fully revalidated package published to nuget.org.
- The consumer smoke test can never silently test a previously published package instead of the freshly packed one.
- No long-lived publishing credentials stored in the repository.

**Non-Goals:**

- Creating a GitHub Release with changelog notes (can be added to the release workflow later).
- Automatic version derivation (MinVer or similar); versioning stays manual in the csproj.
- Multi-platform test matrix; a single Linux runner is sufficient for a managed-only library.
- Guarding that tagged commits are ancestors of master; full revalidation makes this low-risk.

## Decisions

### Two workflows with duplicated steps, single sequential job each

`ci.yml` (triggers: `pull_request`, `push` to master) and `release.yml` (trigger: `push` tags `v*`) each contain one job with the same validation steps: checkout, setup .NET 10, build, unit tests, pack into `artifacts/`, consumer tests. The release workflow adds a version guard before and a publish step after.

- Pack must precede the consumer test because the consumer restores from `artifacts/`; a single sequential job models this dependency directly, with no artifact upload/download between jobs.
- The ~6 shared steps are duplicated rather than extracted into a reusable workflow. At this size, duplication is easier to read and the two workflows are expected to diverge (guard and publish steps exist only in release).

Alternative considered: one workflow with conditional publish steps (`if: startsWith(github.ref, 'refs/tags/')`). Rejected as harder to read and easier to get wrong than two small explicit files.

### csproj `<Version>` is the source of truth; the tag is only the trigger

The release workflow extracts the version from the tag name (`v0.2.0` → `0.2.0`) and fails fast if it does not exactly equal the csproj `<Version>`. Prerelease tags (`v0.2.0-beta.1`) work through the same string comparison.

Alternatives considered:

- Tag as source of truth (`dotnet pack -p:Version=${tag#v}`): removes the version from the repo and makes local packs meaningless.
- MinVer: automatic and elegant, but a new tool dependency for a problem a one-line guard solves.

The release ritual is: bump `<Version>` in a PR, merge, tag the merge commit `v<version>`, push the tag.

### Publish fails loudly on duplicates

`dotnet nuget push` runs without `--skip-duplicate`. Under tag-driven releases, pushing a tag for an already-published version is a mistake (tag without csproj bump, or re-tag) and must fail visibly, not be swallowed.

### Consumer floats its SimpleVerify reference and maps it to the local feed

`SimpleVerify.PackageConsumer.csproj` changes its pin to `Version="*-*"`, and its `nuget.config` gains package source mapping: `SimpleVerify` resolves exclusively from `local-artifacts`, everything else from nuget.org. The float must be `*-*` rather than `*`: a plain `*` matches only stable versions, so a prerelease release (`v0.2.0-beta.1`) would leave the feed containing only a prerelease nupkg and the consumer restore would fail.

Without this, the moment any SimpleVerify version exists on nuget.org, a stale consumer pin would restore the published package instead of the freshly packed one and the smoke test would silently stop testing new code. Source mapping converts that silent failure into a loud restore error. Floating is deterministic in CI because the gitignored `artifacts/` feed contains exactly one nupkg — the one packed moments earlier in the same job.

### Trusted Publishing (OIDC) instead of an API key

The release workflow authenticates to nuget.org via Trusted Publishing: `permissions: id-token: write` plus the `NuGet/login@v1` step that exchanges the GitHub OIDC token for a short-lived push token. No credential is stored or rotated; the only repository secret is `NUGET_USER`, holding the public nuget.org profile name the login step requires. Requires a one-time Trusted Publishing policy on nuget.org naming this repository and the workflow file `release.yml`.

### Runner and toolchain

`ubuntu-latest` with `actions/setup-dotnet` pinned to the `10.0.x` channel. DiffEngine auto-disables diff-tool launching when `CI=true` (set by GitHub Actions), so snapshot mismatches fail tests cleanly without further configuration. Workflows must never set `ContinuousIntegrationBuild=true`.

## Risks / Trade-offs

- [Tag pushed on a commit not on master] → The release workflow revalidates everything itself, so an unmerged tag can at worst publish code that passed the full suite; accepted, no ancestor guard until it proves to be a real problem.
- [Local NuGet global-packages cache serves a stale copy when repacking the same version] → Only affects local development, never fresh CI runners; documented escape hatch is `dotnet nuget locals global-packages --clear` or bumping the version.
- [Floating `Version="*-*"` picks the newest nupkg if a local `artifacts/` accumulates several] → Newest is the intended target during development; CI feeds always contain exactly one package.
- [Trusted Publishing policy is external state not visible in the repo] → Documented in README as a release prerequisite; a misconfigured policy fails the publish step loudly.
- [Version guard compares strings, not SemVer semantics] → Exact-match comparison is intentional; normalization differences (e.g. leading zeros) surface as a failed guard, which is the safe direction.

## Migration Plan

1. Land workflow and consumer-config changes on master (validated by the CI workflow itself on its own PR).
2. Create the GitHub repository, add the remote, push master.
3. Configure the Trusted Publishing policy on nuget.org for this repository and `release.yml`.
4. Tag `v0.1.0` on master and push the tag; the release workflow publishes the first package.
5. Rollback: deleting the workflows restores the status quo; published packages can be unlisted but not deleted from nuget.org.

## Open Questions

- None blocking. GitHub Release creation on tag push is deferred until wanted.
