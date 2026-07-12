# ci-validation

## ADDED Requirements

### Requirement: Pull requests and master pushes are validated

The CI workflow SHALL run on every pull request targeting master and on every push to master, and SHALL fail if the solution does not build or any unit test in `SimpleVerify.Tests` fails.

#### Scenario: Pull request with failing unit test

- **WHEN** a pull request contains a change that breaks a unit test
- **THEN** the CI workflow reports failure on the pull request before merge

#### Scenario: Push to master with passing build and tests

- **WHEN** a commit is pushed to master and the solution builds with all unit tests passing
- **THEN** the CI workflow completes successfully

### Requirement: The packed package is validated by the consumer smoke test

The CI workflow SHALL pack `SimpleVerify` into the local `artifacts/` feed and then run the `SimpleVerify.PackageConsumer` tests, which restore SimpleVerify as a NuGet package from that feed. Packing MUST complete before the consumer test restores.

#### Scenario: Consumer test runs against the freshly packed package

- **WHEN** the CI workflow reaches the consumer test step
- **THEN** the SimpleVerify package restored by the consumer project is the nupkg packed earlier in the same workflow run

#### Scenario: Package is broken despite passing unit tests

- **WHEN** a change passes unit tests but produces a nupkg the consumer project cannot restore or use
- **THEN** the CI workflow fails at the consumer test step

### Requirement: The consumer resolves SimpleVerify exclusively from the local artifacts feed

The consumer project's NuGet configuration SHALL map the `SimpleVerify` package to the `local-artifacts` source only, and its package reference SHALL float so it resolves whatever version was packed. Restore MUST fail if no SimpleVerify nupkg is present in the local feed rather than falling back to nuget.org.

#### Scenario: SimpleVerify exists on nuget.org

- **WHEN** a SimpleVerify version is published on nuget.org and the consumer project restores
- **THEN** the restored SimpleVerify package comes from the local `artifacts/` feed, never from nuget.org

#### Scenario: Local feed is empty

- **WHEN** the consumer project restores without a prior pack step
- **THEN** restore fails with an error instead of silently resolving SimpleVerify from nuget.org

### Requirement: CI never publishes

The CI workflow SHALL NOT push packages to any external feed regardless of trigger or outcome.

#### Scenario: Successful master build

- **WHEN** the CI workflow completes successfully on a master push
- **THEN** no package has been pushed to nuget.org or any other external feed
