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

`VerifySettings.ScrubInlineDateTimes(string format)` and `ScrubInlineDateTimes(string format, CultureInfo culture)` SHALL replace substrings inside string values that parse under the given format (and culture) with date counter tokens.

#### Scenario: Formatted date embedded in a string value

- **WHEN** settings call `ScrubInlineDateTimes` with a format and a string value contains a substring matching that format
- **THEN** the substring is replaced with its `DateTime_N` token

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

The library SHALL unconditionally replace occurrences of the consuming project's solution and project directory paths in the rendered output with `{SolutionDirectory}` and `{ProjectDirectory}` tokens, consuming the trailing separator. Matching SHALL succeed regardless of directory-separator style (`\` or `/`). This replacement SHALL run after all user-registered scrubbers and before comparison, and the directory values SHALL be sourced from assembly metadata embedded by the package's MSBuild targets.

#### Scenario: Stack trace containing the solution directory

- **WHEN** rendered output contains an exception stack trace with source paths under the solution directory
- **THEN** the compared output reads `{SolutionDirectory}<relative-path>` in place of the absolute path and its trailing separator

#### Scenario: Separator style altered by a user scrubber

- **WHEN** a registered `ScrubLinesWithReplace` scrubber rewrites `\` to `/` in path lines before directory replacement runs
- **THEN** the forward-slash form of the solution directory still matches and is replaced with `{SolutionDirectory}`

### Requirement: Scrubbing applies to every entry point

Registered scrubbers SHALL apply to the rendered text of all verification entry points — serialized objects, string targets, and `VerifyJson` — before comparison against the verified file.

#### Scenario: Scrubber applies to VerifyJson output

- **WHEN** a scrubber is registered and `VerifyJson` renders its output
- **THEN** the scrubber runs against that output before comparison
