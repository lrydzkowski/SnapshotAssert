# release-publishing

## ADDED Requirements

### Requirement: A version tag triggers a fully revalidated release

The release workflow SHALL run when a tag matching `v*` is pushed, and SHALL execute the complete validation sequence (build, unit tests, pack, consumer smoke test) on the tagged commit before any publish step. A validation failure MUST prevent publishing.

#### Scenario: Tag on a commit with passing validation

- **WHEN** tag `v0.2.0` is pushed and the tagged commit passes build, unit tests, pack, and consumer tests
- **THEN** the workflow proceeds to publish the packed nupkg

#### Scenario: Tag on a commit with a failing test

- **WHEN** a `v*` tag is pushed and any validation step fails
- **THEN** the workflow fails and nothing is published to nuget.org

### Requirement: The tag version must match the csproj version

The release workflow SHALL extract the version from the tag name by stripping the `v` prefix and SHALL fail before validation if it does not exactly equal the `<Version>` in `SimpleVerify.csproj`. The failure message MUST state both versions.

#### Scenario: Tag and csproj agree

- **WHEN** tag `v0.2.0` is pushed and the csproj `<Version>` is `0.2.0`
- **THEN** the guard passes and the workflow continues

#### Scenario: Tag and csproj disagree

- **WHEN** tag `v0.2.0` is pushed and the csproj `<Version>` is `0.1.0`
- **THEN** the workflow fails immediately with a message naming both `0.2.0` and `0.1.0`

#### Scenario: Prerelease tag

- **WHEN** tag `v0.2.0-beta.1` is pushed and the csproj `<Version>` is `0.2.0-beta.1`
- **THEN** the guard passes and the package is published as a prerelease

### Requirement: Publishing uses Trusted Publishing with no stored credentials

The release workflow SHALL authenticate to nuget.org by exchanging the GitHub Actions OIDC token for a short-lived push token via NuGet Trusted Publishing. The repository SHALL NOT store a long-lived NuGet API key as a secret.

#### Scenario: Publish with valid trusted publishing policy

- **WHEN** the release workflow publishes and a Trusted Publishing policy exists on nuget.org for this repository and workflow
- **THEN** the nupkg packed in the same run is pushed to nuget.org without any stored API key

#### Scenario: Missing or misconfigured policy

- **WHEN** the release workflow publishes without a matching Trusted Publishing policy
- **THEN** the publish step fails with an authentication error

### Requirement: Duplicate versions fail loudly

The publish step SHALL NOT skip duplicates. Attempting to publish a version that already exists on nuget.org MUST fail the workflow.

#### Scenario: Re-pushing an already published version

- **WHEN** a `v*` tag is pushed for a version that already exists on nuget.org
- **THEN** the publish step fails and the workflow reports failure
