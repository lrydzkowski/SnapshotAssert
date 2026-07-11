# snapshot-serialization Specification

## Purpose

Define how object graphs, strings, and JSON inputs are rendered into Verify-compatible snapshot text, including member selection, ordering, null/default handling, and reference-cycle behavior.

## Requirements

### Requirement: Verify-compatible default text format

The library SHALL render object graphs in Verify's default object-graph text format, byte-for-byte compatible with Verify 31.x for the supported feature set: property lines as `Name: value` with unquoted names and unquoted, unescaped string values; objects delimited by `{`/`}` and collections by `[`/`]` on their own lines; members separated by trailing commas; 2-space indentation per nesting level; `\n` line endings.

#### Scenario: Simple object

- **WHEN** an object with properties `Name = "John"` and `Age = 30` is serialized
- **THEN** the output is exactly `{\n  Name: John,\n  Age: 30\n}`

#### Scenario: Nested objects and collections

- **WHEN** an object containing a nested object and a non-empty list is serialized
- **THEN** nested structures render as indented `{...}` and `[...]` blocks identical to Verify's output for the same graph

### Requirement: Scalar rendering rules

The library SHALL render scalars as Verify does: enum values by name; numbers with invariant culture; `false` booleans included even when default-value ignoring is active; `byte[]` as base64; `TimeSpan` via `ToString()`; `null` as the literal `null` when null members are included. Typed `Guid`, `DateTime`, and `DateTimeOffset` rendering is governed by the snapshot-scrubbing capability.

#### Scenario: Enum rendered as name

- **WHEN** a property of type `HttpStatusCode` with value `OK` is serialized
- **THEN** the output line is `StatusCode: OK`

#### Scenario: False boolean survives default-value ignoring

- **WHEN** an object with a `bool` property equal to `false` is serialized with default settings
- **THEN** the property is present in the output as `<Name>: false`

### Requirement: Multi-line string rendering

A string value containing `\n` SHALL be rendered verbatim (no quoting, no escaping, no re-indentation) starting on the line following its property name, reproducing Verify's raw-block behavior for embedded multi-line content such as pretty-printed JSON response bodies.

#### Scenario: Multi-line response body

- **WHEN** a string property whose value contains newlines is serialized
- **THEN** the property name is followed by a line break and the value's lines appear verbatim in the output

### Requirement: Member selection and ordering

The library SHALL serialize public instance properties and fields discovered via System.Text.Json contract metadata, emitting base-type members before derived-type members and preserving declaration order within each type, matching Json.NET's ordering that existing snapshots encode.

#### Scenario: Inherited members ordered first

- **WHEN** an object whose type inherits properties from a base class is serialized
- **THEN** base-class members appear before the derived class's own members, each group in declaration order

### Requirement: Null, default, and empty-collection handling

By default the library SHALL omit members whose value is null, equals the member type's default value (except `bool`), or is an empty collection. `VerifySettings.AddExtraSettings` SHALL allow setting `NullValueHandling.Include` and `DefaultValueHandling.Include`, and `VerifySettings.DontIgnoreEmptyCollections()` SHALL include empty collections, matching the semantics TMHE's settings builders configure today.

#### Scenario: Defaults omit null and empty members

- **WHEN** an object with a null property and an empty list property is serialized with default settings
- **THEN** neither member appears in the output

#### Scenario: TMHE include-everything configuration

- **WHEN** settings configure `NullValueHandling.Include`, `DefaultValueHandling.Include`, and `DontIgnoreEmptyCollections()`
- **THEN** null members render as `null`, default-valued members are present, and empty collections render as `[]`

### Requirement: Member ignoring by name

`VerifySettings.IgnoreMember(string name)` SHALL omit every member whose name matches the given name from the serialized output.

#### Scenario: Ignored member omitted

- **WHEN** settings call `IgnoreMember("errorStackTrace")` and the target graph contains a member with that name
- **THEN** the member does not appear in the output

### Requirement: Dictionary ordering

Dictionaries SHALL be serialized with entries ordered by key using ordinal comparison, matching Verify's default `OrderDictionaries` behavior.

#### Scenario: Unordered dictionary input

- **WHEN** a dictionary with keys inserted in non-alphabetical order is serialized
- **THEN** entries appear sorted by key

### Requirement: VerifyJson string input

`Verifier.VerifyJson(string json)` SHALL parse the JSON string and render the resulting structure in the default object-graph text format, equivalent to Verify's `VerifyJson` output for the same input. Invalid JSON SHALL fail with a descriptive parse error.

#### Scenario: JSON string verified

- **WHEN** a test awaits `VerifyJson("{\"key\": \"value\"}")`
- **THEN** the output is the object-graph rendering `{\n  key: value\n}`

#### Scenario: Malformed JSON input

- **WHEN** `VerifyJson` receives a string that is not valid JSON
- **THEN** the verification fails with an error describing the parse failure

### Requirement: Reference cycle handling matches Verify

A member whose value is the same instance as the object declaring it SHALL render as `$parentValue`. A member whose serialization would revisit an instance already on the current serialization path (closing a reference cycle) SHALL be silently omitted. Shared references that do not close a cycle SHALL serialize in full at each occurrence.

#### Scenario: Direct self-reference

- **WHEN** an object with a member referencing the object itself is serialized
- **THEN** that member renders as `<Name>: $parentValue`

#### Scenario: Indirect cycle silently omitted

- **WHEN** a graph is serialized in which a nested member references an ancestor on the current serialization path
- **THEN** that member is omitted from the output and no exception is thrown

#### Scenario: Shared reference is not a cycle

- **WHEN** the same instance appears in two sibling members of a graph
- **THEN** both occurrences serialize in full
