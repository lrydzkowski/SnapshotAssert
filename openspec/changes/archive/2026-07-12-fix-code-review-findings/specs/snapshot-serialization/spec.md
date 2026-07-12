# snapshot-serialization Delta

## ADDED Requirements

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

### Requirement: Whitespace-safe output format

Rendered output SHALL contain no line ending in a space or tab character, so editors configured to trim trailing whitespace cannot alter `.verified.txt` files. An empty string property value SHALL render as the property name and colon with nothing after the colon (`Name:`). An empty string array element SHALL contribute no characters of its own: its line is completely blank when it is the final element, or holds only the following element's separator comma otherwise. Property names containing `\n` or `\r` SHALL have those characters escaped so a name cannot span or break lines.

#### Scenario: Empty string property has no trailing space

- **WHEN** an object with a string property equal to `""` is serialized with null/default handling that includes it
- **THEN** the property renders as `Name:` with no trailing whitespace

#### Scenario: Empty string in array renders without whitespace

- **WHEN** the array `["a", "", "b"]` is serialized
- **THEN** the middle element renders as a line holding only the next element's separator comma, with no indentation characters

#### Scenario: Trailing empty string in array renders as blank line

- **WHEN** the array `["a", ""]` is serialized
- **THEN** the final element renders as a completely blank line

#### Scenario: Dictionary key containing a newline

- **WHEN** a dictionary with a string key containing `\n` is serialized
- **THEN** the rendered key occupies a single line with the newline escaped

## MODIFIED Requirements

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
