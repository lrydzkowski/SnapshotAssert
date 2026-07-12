# release-publishing Delta

## MODIFIED Requirements

### Requirement: The tag version must match the csproj version

The release workflow SHALL extract the version from the tag name by stripping the `v` prefix and SHALL fail before validation if it does not exactly equal the `<Version>` in `SnapshotAssert.csproj`. The failure message MUST state both versions.

#### Scenario: Tag and csproj agree

- **WHEN** tag `v0.2.0` is pushed and the csproj `<Version>` is `0.2.0`
- **THEN** the guard passes and the workflow continues

#### Scenario: Tag and csproj disagree

- **WHEN** tag `v0.2.0` is pushed and the csproj `<Version>` is `0.1.0`
- **THEN** the workflow fails immediately with a message naming both `0.2.0` and `0.1.0`

#### Scenario: Prerelease tag

- **WHEN** tag `v0.2.0-beta.1` is pushed and the csproj `<Version>` is `0.2.0-beta.1`
- **THEN** the guard passes and the package is published as a prerelease
