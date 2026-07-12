# snapshot-scrubbing Delta

## ADDED Requirements

### Requirement: Culture-invariant string date recognition

String values that exactly match one of the library's recognized date formats SHALL be replaced with date counter tokens using `CultureInfo.InvariantCulture` for all parsing. The recognized formats are the ISO date-time format `yyyy-MM-ddTHH:mm:ss.FFFFFFFK` (as `DateTime` and `DateTimeOffset`), the ISO date format `yyyy-MM-dd`, and the invariant short date format (`MM/dd/yyyy`). Recognition SHALL be independent of the machine's current culture: the same input string SHALL produce the same scrubbed output on every machine.

#### Scenario: ISO date string scrubbed

- **WHEN** a string value `2026-07-12` is serialized with default settings
- **THEN** it renders as a `DateTime_N` counter token

#### Scenario: Invariant short date string scrubbed

- **WHEN** a string value `07/12/2026` is serialized with default settings
- **THEN** it renders as a `DateTime_N` counter token

#### Scenario: Identical output across cultures

- **WHEN** the same target containing date-shaped string values is serialized under `en-US` and under `de-DE`
- **THEN** the rendered outputs are byte-identical

#### Scenario: DateOnly value and equivalent date string share a token

- **WHEN** a target contains a `DateOnly` property and a string property holding the ISO form of the same date
- **THEN** both render as the same `DateTime_N` token

### Requirement: Single scrubbing pass per verification

Registered instance scrubbers and the always-on directory replacement SHALL each be applied exactly once per verification, against the fully rendered document. Per-value processing during serialization SHALL be limited to counter-based inline conversions and newline normalization.

#### Scenario: Non-idempotent scrubber applied once

- **WHEN** settings register a scrubber replacing `a` with `aa` and a string property value contains `a`
- **THEN** the compared output contains `aa` (not `aaaa`) for that occurrence

#### Scenario: Line scrubber sees rendered lines

- **WHEN** settings call `ScrubLinesContaining("secret")` and a property renders as the line `Token: secret-value`
- **THEN** the entire rendered line is removed from the compared output

## MODIFIED Requirements

### Requirement: Inline date scrubbing in strings

`VerifySettings.ScrubInlineDateTimes(string format)` and `ScrubInlineDateTimes(string format, CultureInfo culture)` SHALL replace substrings inside string values that parse under the given format (and culture) with date counter tokens. When no culture is supplied, parsing SHALL use `CultureInfo.InvariantCulture`, so the scrubber's behavior is independent of the machine's current culture.

#### Scenario: Formatted date embedded in a string value

- **WHEN** settings call `ScrubInlineDateTimes` with a format and a string value contains a substring matching that format
- **THEN** the substring is replaced with its `DateTime_N` token

#### Scenario: Default culture is invariant

- **WHEN** settings call `ScrubInlineDateTimes("dd MMM yyyy")` without a culture on machines with different current cultures
- **THEN** the same input produces the same scrubbed output on every machine, using invariant month names

### Requirement: Always-on directory path replacement

The library SHALL unconditionally replace occurrences of the consuming project's solution and project directory paths in the rendered output with `{SolutionDirectory}` and `{ProjectDirectory}` tokens, consuming the trailing separator. Matching SHALL succeed regardless of directory-separator style (`\` or `/`). This replacement SHALL run after all user-registered scrubbers and before comparison, and the directory values SHALL be sourced from assembly metadata embedded by the package's MSBuild targets. Initialization of the replacement list from assembly metadata SHALL complete before any verification renders output, including when multiple verifications start concurrently on first use, so no verification ever scrubs with a partially initialized replacement list.

#### Scenario: Stack trace containing the solution directory

- **WHEN** rendered output contains an exception stack trace with source paths under the solution directory
- **THEN** the compared output reads `{SolutionDirectory}<relative-path>` in place of the absolute path and its trailing separator

#### Scenario: Separator style altered by a user scrubber

- **WHEN** a registered `ScrubLinesWithReplace` scrubber rewrites `\` to `/` in path lines before directory replacement runs
- **THEN** the forward-slash form of the solution directory still matches and is replaced with `{SolutionDirectory}`

#### Scenario: Concurrent first verifications

- **WHEN** two verifications call `Verify` concurrently as the first verifications in the process
- **THEN** both scrub with the fully initialized replacement list including `{ProjectDirectory}` and `{SolutionDirectory}`
