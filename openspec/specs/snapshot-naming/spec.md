# snapshot-naming Specification

## Purpose

Define how snapshot file names and locations are derived from test identity, how `UseParameters` and `UseFileName` customize them, and how naming collisions are prevented.

## Requirements

### Requirement: Default file naming next to the test

The library SHALL derive the snapshot file-name prefix as `{TestClassName}.{TestMethodName}` (including containing-type names for nested classes) and place snapshot files in the directory of the test's source file, producing `{prefix}.received.txt` and `{prefix}.verified.txt`, identical to Verify's default so existing files resolve unchanged.

#### Scenario: Default naming

- **WHEN** test method `GetSites_ShouldReturnGivenSitesResponse` in class `GetSitesTests` awaits a verification
- **THEN** the compared file is `GetSitesTests.GetSites_ShouldReturnGivenSitesResponse.verified.txt` in the test source file's directory

### Requirement: UseParameters appends parameter segments

`UseParameters(params object?[] values)` SHALL append `_{parameterName}={value}` segments to the file-name prefix, pairing the given values with the test method's parameter names in declaration order. When fewer values than parameters are supplied, the first N parameter names SHALL be used; supplying more values than parameters SHALL fail with a descriptive error. Value formatting SHALL match Verify's (invariant culture).

#### Scenario: Single parameter segment

- **WHEN** a theory with parameter `testCase` awaits `Verify(result, settings).UseParameters(testCase.TestCaseId.ToString("D3"))`
- **THEN** the file-name prefix ends with `_testCase=001` for test case id 1

#### Scenario: Too many parameter values

- **WHEN** `UseParameters` receives more values than the test method has parameters
- **THEN** the verification fails with an error describing the mismatch

### Requirement: Parameterized tests must disambiguate

When the current test method has parameters and neither `UseParameters` nor `UseFileName` was called, the verification SHALL fail with a descriptive error instructing the author to call one of them, instead of silently sharing one snapshot across theory cases.

#### Scenario: Theory without UseParameters

- **WHEN** a parameterized test awaits a verification without calling `UseParameters` or `UseFileName`
- **THEN** the verification fails with an error naming the required calls

### Requirement: UseFileName overrides the prefix

`UseFileName(string fileName)` SHALL replace the derived `{TestClassName}.{TestMethodName}` prefix with the given name for both received and verified files.

#### Scenario: Explicit file name

- **WHEN** a verification calls `UseFileName("SavePowerBiReportsConfiguration")`
- **THEN** the compared file is `SavePowerBiReportsConfiguration.verified.txt` in the test source file's directory

### Requirement: Unique prefix guard

The library SHALL fail with a descriptive error when two verifications within one test run resolve to the same file-name prefix, preventing one snapshot from silently overwriting another.

#### Scenario: Two verifications share a prefix

- **WHEN** a second verification in the same run resolves to a prefix already used
- **THEN** it fails with an error identifying the colliding prefix and suggesting `UseParameters`/`UseFileName`
