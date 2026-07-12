# snapshot-serialization Delta

## MODIFIED Requirements

### Requirement: Verify-compatible default text format

The library SHALL render object graphs in Verify's default object-graph text format, byte-for-byte compatible with Verify 31.x for the supported feature set: property lines as `Name: value` with unquoted names and unquoted, unescaped string values; objects delimited by `{`/`}` and collections by `[`/`]` on their own lines; members separated by trailing commas; 2-space indentation per nesting level; `\n` line endings. Empty string values SHALL render exactly as Verify 31.x renders them: in object context as the property name, colon, and separator space followed by nothing (`Name: ` with a trailing space); in array context as an element line carrying the normal indentation followed by nothing.

#### Scenario: Simple object

- **WHEN** an object with properties `Name = "John"` and `Age = 30` is serialized
- **THEN** the output is exactly `{\n  Name: John,\n  Age: 30\n}`

#### Scenario: Nested objects and collections

- **WHEN** an object containing a nested object and a non-empty list is serialized
- **THEN** nested structures render as indented `{...}` and `[...]` blocks identical to Verify's output for the same graph

#### Scenario: Empty string property value

- **WHEN** an object with a string property equal to `""` is serialized with null/default handling that includes it
- **THEN** the property renders as `Name: ` — colon and separator space with nothing after, the line ending in the space

#### Scenario: Empty string between array items

- **WHEN** the array `["a", "", "b"]` is serialized
- **THEN** the output is exactly `[\n  a,\n  ,\n  b\n]`, the middle element's line holding its indentation followed by the next element's separator comma

#### Scenario: Empty string as final array item

- **WHEN** the array `["a", ""]` is serialized
- **THEN** the output is exactly `[\n  a,\n  \n]`, the final element's line holding only its indentation

## ADDED Requirements

### Requirement: Single-line property names

Property names containing `\n` or `\r` SHALL have those characters escaped so a rendered name always occupies a single line and cannot break the line structure of the output.

#### Scenario: Dictionary key containing a newline

- **WHEN** a dictionary with a string key containing `\n` is serialized
- **THEN** the rendered key occupies a single line with the newline escaped

## REMOVED Requirements

### Requirement: Whitespace-safe output format

**Reason**: The no-trailing-whitespace guarantee for empty values broke byte-for-byte compatibility with Verify 31.x snapshots (`Name: ` became `Name:`), failing every existing consumer snapshot containing an empty string value. Verify compatibility is the library's core promise and wins over editor-trim safety.
**Migration**: Empty string values render with Verify-compatible whitespace again (see the modified "Verify-compatible default text format" requirement). Snapshots approved against the no-trailing-whitespace format must be re-approved. To protect snapshot files from trim-on-save editors, configure `.editorconfig` with `trim_trailing_whitespace = false` for `*.verified.txt` and `*.received.txt`. The name-escaping half of the removed requirement survives unchanged as "Single-line property names".
