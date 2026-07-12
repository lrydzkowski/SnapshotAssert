# snapshot-scrubbing Specification

## Purpose

Define how nondeterministic values (guids, dates, machine-specific paths) and user-selected content are scrubbed from rendered output before comparison, matching Verify's scrubbing semantics.

## Requirements

### Requirement: Typed Guid counter scrubbing

By default, typed `Guid` values SHALL be replaced with counter tokens `Guid_1`, `Guid_2`, … numbered by first occurrence within a single verification, with identical values always mapping to the same token. `VerifySettings.DontScrubGuids()` SHALL disable this, rendering the literal lowercase hyphenated value. Token spelling and edge cases SHALL match Verify 31.x.

#### Scenario: Repeated Guid maps to one token

- **WHEN** an object containing the same `Guid` value in two members and a different `Guid` in a third is serialized
- **THEN** the first two render as `Guid_1` and the third as `Guid_2`

#### Scenario: Guid scrubbing disabled

- **WHEN** settings call `DontScrubGuids()` and a `Guid` property is serialized
- **THEN** the literal guid value appears in the output

### Requirement: Typed date counter scrubbing

By default, typed `DateTime` and `DateTimeOffset` values SHALL be replaced with counter tokens (`DateTime_1`, `DateTimeOffset_1`, …) numbered by first occurrence within a single verification, with identical values mapping to the same token. `VerifySettings.DontScrubDateTimes()` SHALL disable this, rendering the value in Verify's date format (ported from Verify's `DateFormatter`). Token spelling SHALL match Verify 31.x.

#### Scenario: DateTime scrubbed by default

- **WHEN** an object with two distinct `DateTime` values is serialized with default settings
- **THEN** they render as `DateTime_1` and `DateTime_2`

#### Scenario: Date scrubbing disabled

- **WHEN** settings call `DontScrubDateTimes()` and a `DateTime` property is serialized
- **THEN** the value renders in Verify's literal date format

### Requirement: Inline Guid scrubbing in strings

`VerifySettings.ScrubInlineGuids()` SHALL replace guid-shaped substrings inside string values with counter tokens drawn from the same counter as typed `Guid` scrubbing, so a guid appearing both typed and embedded in text yields the same token.

#### Scenario: Guid embedded in a string value

- **WHEN** settings call `ScrubInlineGuids()` and a string value contains a guid substring
- **THEN** the substring is replaced with its `Guid_N` token

### Requirement: Inline date scrubbing in strings

`VerifySettings.ScrubInlineDateTimes(string format)` and `ScrubInlineDateTimes(string format, CultureInfo culture)` SHALL replace substrings inside string values that parse under the given format (and culture) with date counter tokens. When no culture is supplied, parsing SHALL use `CultureInfo.InvariantCulture`, so the scrubber's behavior is independent of the machine's current culture.

#### Scenario: Formatted date embedded in a string value

- **WHEN** settings call `ScrubInlineDateTimes` with a format and a string value contains a substring matching that format
- **THEN** the substring is replaced with its `DateTime_N` token

#### Scenario: Default culture is invariant

- **WHEN** settings call `ScrubInlineDateTimes("dd MMM yyyy")` without a culture on machines with different current cultures
- **THEN** the same input produces the same scrubbed output on every machine, using invariant month names

### Requirement: Line-based scrubbers

`VerifySettings.ScrubLinesContaining(params string[] stringToMatch)` SHALL remove every output line containing any given substring (ordinal case-insensitive). `VerifySettings.ScrubLinesWithReplace(Func<string, string?> replaceLine)` SHALL apply the function to every line, replacing the line with the returned value and removing the line entirely when the function returns null.

#### Scenario: Lines containing a marker removed

- **WHEN** settings call `ScrubLinesContaining("traceId")` and the rendered output contains lines with `traceId`
- **THEN** those lines are absent from the compared output

#### Scenario: Line transformation applied

- **WHEN** settings call `ScrubLinesWithReplace` with a function rewriting matching lines
- **THEN** every line of the output has passed through the function before comparison

### Requirement: Custom whole-text scrubbers

`VerifySettings.AddScrubber(Action<StringBuilder>)` SHALL register a scrubber invoked against the entire rendered output before comparison. Scrubbers SHALL execute with Verify's default ordering semantics: each registration is inserted at the front of the scrubber list, so the most recently registered scrubber runs first.

#### Scenario: Regex scrubber mutates output

- **WHEN** settings register a scrubber that regex-replaces URLs in the `StringBuilder`
- **THEN** the compared output contains the replaced form

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

### Requirement: Scrubbing applies to every entry point

Registered scrubbers SHALL apply to the rendered text of all verification entry points — serialized objects, string targets, and `VerifyJson` — before comparison against the verified file.

#### Scenario: Scrubber applies to VerifyJson output

- **WHEN** a scrubber is registered and `VerifyJson` renders its output
- **THEN** the scrubber runs against that output before comparison

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
