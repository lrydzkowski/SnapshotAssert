# snapshot-serialization Delta

## ADDED Requirements

### Requirement: Member ignoring by type

`VerifySettings.IgnoreMembersWithType<T>()` and `VerifySettings.IgnoreMembersWithType(Type type)` SHALL omit every member whose declared type or runtime value type is assignable to any registered ignored type. Declared types SHALL be unwrapped from `Nullable<T>` before matching. `System.IO.Stream` SHALL be registered as an ignored type by default. Members of an ignored type SHALL be omitted entirely, including when their value is null and null handling would otherwise render them. Ignoring by type SHALL apply to object members only, not to array items or dictionary entries, matching the scope of `IgnoreMember(string)`.

#### Scenario: Member of ignored type omitted

- **WHEN** settings call `IgnoreMembersWithType<IntPtr>()` and the target graph contains a member of declared type `IntPtr`
- **THEN** the member does not appear in the output and no exception is thrown

#### Scenario: Assignability covers derived types

- **WHEN** settings call `IgnoreMembersWithType(typeof(Stream))` and a member's declared type is `FileStream`
- **THEN** the member is omitted

#### Scenario: Runtime type is matched behind a broader declared type

- **WHEN** a member declared as `object` holds a `MemoryStream` instance and default settings are used
- **THEN** the member is omitted because `Stream` is ignored by default

#### Scenario: Nullable declared type is unwrapped

- **WHEN** settings call `IgnoreMembersWithType<IntPtr>()` and a member's declared type is `IntPtr?`
- **THEN** the member is omitted

#### Scenario: Ignored types survive settings cloning

- **WHEN** a settings instance with `IgnoreMembersWithType<IntPtr>()` is used as the parent of a derived settings instance
- **THEN** the derived instance also omits members of type `IntPtr`

## MODIFIED Requirements

### Requirement: Converter-backed scalar type rendering

The library SHALL render `DateOnly` values through the date counter (sharing tokens with `DateTime` values at midnight of the same date, and honoring `DontScrubDateTimes()`), `TimeOnly` values as `HH:mm:ss.FFFFFFF` with invariant culture, `Uri` values as their `OriginalString`, and `Version` values via `ToString()`. `DateOnly` and `TimeOnly` dictionary keys SHALL render the same way. The library SHALL additionally render `Int128`, `UInt128`, and `BigInteger` values as invariant-culture integers; `Half` values as invariant-culture floating-point numbers with the same decimal-place normalization as `float` and `double` (including `NaN` and infinity literals); `Memory<byte>` and `ReadOnlyMemory<byte>` values as base64, identical to `byte[]`; and `JsonElement` and `JsonDocument` values as structured content with the same treatment as `JsonNode` (object properties in document order, string values subject to scrubbing and counters, an `Undefined` element as `null`). Any other type whose serialization contract resolves to no members and no known kind (`JsonTypeInfoKind.None`), except `System.Object` itself, SHALL fail the verification with a descriptive `VerifyException` that names the type and points to `IgnoreMembersWithType` as the escape hatch, instead of silently rendering `{}`.

#### Scenario: DateOnly scrubbed by default

- **WHEN** an object with a `DateOnly` property is serialized with default settings
- **THEN** the property renders as a `DateTime_N` counter token

#### Scenario: Uri renders its original string

- **WHEN** an object with property `Endpoint = new Uri("https://example.test/api")` is serialized
- **THEN** the output line is `Endpoint: https://example.test/api`

#### Scenario: Version renders its value

- **WHEN** an object with property `Version = new Version(1, 2, 3)` is serialized
- **THEN** the output line is `Version: 1.2.3`

#### Scenario: Int128 renders as a number

- **WHEN** an object with an `Int128` property equal to `170141183460469231731687303715884105727` is serialized
- **THEN** the property renders as `170141183460469231731687303715884105727`

#### Scenario: Half renders like float

- **WHEN** an object with a `Half` property equal to `(Half)1.5` and another equal to `(Half)2` is serialized
- **THEN** the properties render as `1.5` and `2.0`

#### Scenario: BigInteger renders as a number

- **WHEN** an object with a `BigInteger` property equal to `BigInteger.Parse("12345678901234567890")` is serialized
- **THEN** the property renders as `12345678901234567890`, not as an object of `BigInteger`'s computed properties

#### Scenario: ReadOnlyMemory of bytes renders as base64

- **WHEN** an object with a `ReadOnlyMemory<byte>` property over the bytes `[1, 2]` is serialized
- **THEN** the property renders as `AQI=`

#### Scenario: JsonElement renders structured content

- **WHEN** an object with a `JsonElement` property holding the parsed document `{"key": "value"}` is serialized
- **THEN** the property renders as the object-graph block `{\n  key: value\n}` and string values inside it are subject to scrubbing

#### Scenario: Unknown contract-less type fails fast with escape hatch

- **WHEN** an object with a property of a type that resolves to `JsonTypeInfoKind.None` and has no explicit rendering (e.g. `IntPtr`) is serialized without an ignore for that type
- **THEN** the verification fails with a `VerifyException` naming the unsupported type and mentioning `IgnoreMembersWithType`

#### Scenario: Plain object still renders as empty braces

- **WHEN** an object with a property of type `object` holding `new object()` is serialized
- **THEN** the property renders as `{}`
