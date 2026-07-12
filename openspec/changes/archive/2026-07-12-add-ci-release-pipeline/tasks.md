# Tasks: add-ci-release-pipeline

## 1. Harden consumer package resolution

- [x] 1.1 Add package source mapping to `tests/SimpleVerify.PackageConsumer/nuget.config`: `SimpleVerify` maps only to `local-artifacts`, `*` maps to nuget.org
- [x] 1.2 Change the `SimpleVerify` `PackageReference` in `SimpleVerify.PackageConsumer.csproj` from `Version="0.1.0"` to floating `Version="*-*"` (prerelease-inclusive float; plain `*` would miss prerelease-only feeds)
- [x] 1.3 Verify locally: clear `artifacts/`, confirm consumer restore fails on the empty feed, then `dotnet pack` into `artifacts/` and confirm consumer tests restore the fresh nupkg and pass

## 2. CI workflow

- [x] 2.1 Create `.github/workflows/ci.yml` triggered by `pull_request` and `push` to master, with one job on `ubuntu-latest`: checkout, `actions/setup-dotnet` with `10.0.x`, build solution, run `SimpleVerify.Tests`, pack into `artifacts/`, run `SimpleVerify.PackageConsumer` tests
- [x] 2.2 Confirm the workflow contains no publish step and never sets `ContinuousIntegrationBuild`

## 3. Release workflow

- [x] 3.1 Create `.github/workflows/release.yml` triggered by pushing tags matching `v*`, with `permissions: id-token: write`
- [x] 3.2 Add the version guard step: extract the version from the tag name, compare with `<Version>` in `src/SimpleVerify/SimpleVerify.csproj`, fail with a message naming both versions on mismatch
- [x] 3.3 Add the same validation steps as CI: build, unit tests, pack into `artifacts/`, consumer tests
- [x] 3.4 Add the publish steps: NuGet Trusted Publishing login (OIDC token exchange) followed by `dotnet nuget push` of the packed nupkg without `--skip-duplicate`

## 4. Documentation

- [x] 4.1 Document the release process in `README.md`: bump `<Version>` in a PR, merge, tag the merge commit `v<version>`, push the tag
- [x] 4.2 Document release prerequisites in `README.md`: Trusted Publishing policy on nuget.org for this repository and `release.yml`, the `NUGET_USER` repository secret, and the local-cache escape hatch `dotnet nuget locals global-packages --clear`

## 5. External setup and first release

- [x] 5.1 Create the GitHub repository, add it as remote, push master, and confirm the CI workflow runs green
- [x] 5.2 Configure the Trusted Publishing policy on nuget.org for this repository and workflow (the `dotnet:nuget-trusted-publishing` skill walks through this)
- [x] 5.3 Tag `v0.1.0` on master, push the tag, and confirm the release workflow publishes SimpleVerify 0.1.0 to nuget.org
