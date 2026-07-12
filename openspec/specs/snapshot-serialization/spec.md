# snapshot-serialization Specification

## Purpose

Define how object graphs, strings, and JSON inputs are rendered into Verify-compatible snapshot text, including member selection, ordering, null/default handling, and reference-cycle behavior.

## Requirements

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

Dictionaries SHALL be serialized with entries ordered by their rendered key string using `StringComparer.Ordinal`. Keys SHALL be converted to their rendered form (including counter tokens for `Guid`/`DateTime`/`DateTimeOffset` keys) before ordering, so ordering never depends on key types being comparable and never uses culture-sensitive collation. Dictionaries with mixed or non-`IComparable` key types SHALL serialize without error, and key order SHALL be identical on every machine.

#### Scenario: Unordered dictionary input

- **WHEN** a dictionary with keys inserted in non-alphabetical order is serialized
- **THEN** entries appear sorted by key

#### Scenario: Ordinal ordering of string keys

- **WHEN** a dictionary with keys `apple`, `Banana`, `cherry` is serialized
- **THEN** entries appear in ordinal order `Banana`, `apple`, `cherry` regardless of machine culture

#### Scenario: Mixed key types do not crash

- **WHEN** a `Dictionary<object, string>` containing both an `int` key and a `string` key is serialized
- **THEN** the verification renders both entries ordered by their rendered key strings without throwing

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

### Requirement: Converter-backed scalar type rendering

The library SHALL render `DateOnly` values through the date counter (sharing tokens with `DateTime` values at midnight of the same date, and honoring `DontScrubDateTimes()`), `TimeOnly` values as `HH:mm:ss.FFFFFFF` with invariant culture, `Uri` values as their `OriginalString`, and `Version` values via `ToString()`. `DateOnly` and `TimeOnly` dictionary keys SHALL render the same way. Any other type whose serialization contract resolves to no members and no known kind (`JsonTypeInfoKind.None`), except `System.Object` itself, SHALL fail the verification with a descriptive `VerifyException` naming the type instead of silently rendering `{}`.

#### Scenario: DateOnly scrubbed by default

- **WHEN** an object with a `DateOnly` property is serialized with default settings
- **THEN** the property renders as a `DateTime_N` counter token

#### Scenario: Uri renders its original string

- **WHEN** an object with property `Endpoint = new Uri("https://example.test/api")` is serialized
- **THEN** the output line is `Endpoint: https://example.test/api`

#### Scenario: Version renders its value

- **WHEN** an object with property `Version = new Version(1, 2, 3)` is serialized
- **THEN** the output line is `Version: 1.2.3`

#### Scenario: Unknown contract-less type fails fast

- **WHEN** an object with a property of a type that resolves to `JsonTypeInfoKind.None` and has no explicit rendering (e.g. `Int128`) is serialized
- **THEN** the verification fails with a `VerifyException` naming the unsupported type

#### Scenario: Plain object still renders as empty braces

- **WHEN** an object with a property of type `object` holding `new object()` is serialized
- **THEN** the property renders as `{}`

### Requirement: Lazy sequences are enumerated at most once

Members that are `IEnumerable` but not `ICollection` and not dictionary-shaped SHALL be enumerated exactly once per verification: the emptiness check and the array rendering SHALL share a single materialized enumeration. Dictionary-shaped members (implementing `IDictionary` or `IReadOnlyDictionary<,>`) SHALL retain their dictionary rendering.

#### Scenario: Iterator enumerated once

- **WHEN** a member is a lazy iterator that counts its enumerations and the object is serialized
- **THEN** the iterator reports exactly one enumeration and its items appear in the output

#### Scenario: Read-only dictionary member keeps dictionary form

- **WHEN** a member's type implements only `IReadOnlyDictionary<,>` (not `ICollection`) and the object is serialized
- **THEN** the member renders as an object with sorted keys, not as an array of key-value pairs

### Requirement: Single-line property names

Property names containing `\n` or `\r` SHALL have those characters escaped so a rendered name always occupies a single line and cannot break the line structure of the output.

#### Scenario: Dictionary key containing a newline

- **WHEN** a dictionary with a string key containing `\n` is serialized
- **THEN** the rendered key occupies a single line with the newline escaped
