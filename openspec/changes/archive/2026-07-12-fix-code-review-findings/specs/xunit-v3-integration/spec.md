# xunit-v3-integration Delta

## MODIFIED Requirements

### Requirement: Source-file-relative snapshot directory

The library SHALL capture the calling test's source file path via `[CallerFilePath]` on the `Verify`/`VerifyJson` entry points and use its directory as the snapshot directory, matching Verify's placement of snapshots next to the test file. When the resolved snapshot directory does not exist on disk, the verification SHALL fail with a descriptive `VerifyException` before any file operation; when the captured source file path shows PathMap rewriting (a leading `/_`), the error message SHALL name `DeterministicSourcePaths`/`ContinuousIntegrationBuild` as the likely cause and state that they must be disabled for test projects.

#### Scenario: Snapshot lands next to the test source

- **WHEN** a test in `GetAuditLogsTests.cs` awaits a verification
- **THEN** received and verified files are read and written in the directory containing `GetAuditLogsTests.cs`

#### Scenario: PathMap-rewritten caller file path

- **WHEN** a verification runs in a build where `[CallerFilePath]` was rewritten to a `/_/...` path by `DeterministicSourcePaths`
- **THEN** it fails with a `VerifyException` explaining that deterministic source paths must be disabled for test projects, instead of a low-level file-system error
