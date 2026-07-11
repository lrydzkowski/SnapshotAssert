# snapshot-verification Specification

## ADDED Requirements

### Requirement: Awaitable Verify entry points

The library SHALL expose `Verifier.Verify(object? target)` and `Verifier.Verify(object? target, VerifySettings settings)` returning an awaitable verification task. Verification SHALL execute when the task is awaited, and the task SHALL support fluent configuration (`UseParameters`, `UseFileName`) before being awaited.

#### Scenario: Verify with default settings

- **WHEN** a test awaits `Verify(target)` for a serializable object
- **THEN** the target is serialized and compared against the matching `.verified.txt` file

#### Scenario: Verify with explicit settings

- **WHEN** a test awaits `Verify(target, settings)` with a configured `VerifySettings` instance
- **THEN** the settings (scrubbers, serialization options, naming overrides) are applied to that verification only

### Requirement: First verification creates received file and fails

When no `.verified.txt` file exists for the derived file name, the library SHALL write the rendered output to the `.received.txt` file and fail the test with an exception whose message identifies the snapshot directory, lists the received/verified file pair under a `New:` section, and includes the received file content.

#### Scenario: No verified file exists

- **WHEN** a verification runs and `<prefix>.verified.txt` does not exist
- **THEN** `<prefix>.received.txt` is written with the rendered output
- **THEN** the verification throws an exception naming the directory, the new file pair, and the received content

### Requirement: Matching verification passes cleanly

When the rendered output equals the `.verified.txt` content, the verification SHALL pass without throwing, SHALL NOT leave a `.received.txt` file for that prefix on disk, and SHALL close any diff-tool instance previously opened for that file pair.

#### Scenario: Output matches verified file

- **WHEN** a verification's rendered output is identical to the existing `<prefix>.verified.txt`
- **THEN** the test passes and no `<prefix>.received.txt` file remains

### Requirement: Mismatched verification writes received file and reports diff

When the rendered output differs from the `.verified.txt` content, the library SHALL write the `.received.txt` file, launch the configured diff tool via DiffEngine for the received/verified pair, and fail with an exception whose message identifies the directory, lists the pair under a `NotEqual:` section, and includes both received and verified content.

#### Scenario: Output differs from verified file

- **WHEN** a verification's rendered output differs from `<prefix>.verified.txt`
- **THEN** `<prefix>.received.txt` is written, DiffEngine launches the diff tool for the pair, and the thrown exception message contains the directory, the file pair, and both file contents

#### Scenario: Diff suppressed on build servers

- **WHEN** a mismatch occurs and DiffEngine detects a build server or diff launching is disabled
- **THEN** the verification fails with the same exception but no diff tool is launched

### Requirement: Line-ending and encoding discipline

Received files SHALL be written UTF-8 with `\n` line endings only. When reading a `.verified.txt` file that contains a carriage return character, the library SHALL fail with a descriptive error instructing that verified files must use `\n` line endings.

#### Scenario: Verified file contains CRLF

- **WHEN** a verification reads a `.verified.txt` file containing `\r`
- **THEN** the verification fails with an error identifying the file and the line-ending requirement

### Requirement: Stale received files are removed before verification

Before comparing, the library SHALL delete any existing `.received.*` files matching the verification's file-name prefix, so leftover files from earlier runs never survive a passing test.

#### Scenario: Leftover received file from previous failing run

- **WHEN** a verification starts and `<prefix>.received.txt` exists from a previous run
- **THEN** the stale file is deleted before the new comparison executes

### Requirement: Empty string target convention

Verifying an empty string SHALL produce the literal file content `emptyString`, matching Verify's convention so existing snapshots of empty results keep passing.

#### Scenario: Empty string verified

- **WHEN** a test awaits `Verify(string.Empty)`
- **THEN** the rendered output is exactly `emptyString`
