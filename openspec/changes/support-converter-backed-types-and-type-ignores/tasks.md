# Tasks: support-converter-backed-types-and-type-ignores

## 1. Member ignoring by type

- [x] 1.1 Add `internal HashSet<Type> IgnoredMemberTypes` to `SerializationSettings`, seeded with `typeof(Stream)` and copied in `Clone()`
- [x] 1.2 Add `VerifySettings.IgnoreMembersWithType<T>()` and `IgnoreMembersWithType(Type type)` (null-validated) delegating to `Serialization.IgnoredMemberTypes`
- [x] 1.3 In `ObjectWalker.WriteMember`, replace the hardcoded `Stream` check with a type-ignore check: match any ignored type by assignability against the `Nullable`-unwrapped declared type or the runtime value type, placed after the name-based ignore and before null handling
- [x] 1.4 Add tests: `IntPtr` member ignored via generic and non-generic overloads; `FileStream`-declared member caught by `Stream` assignability; `MemoryStream` behind an `object`-declared member omitted by default; `IntPtr?` declared member caught via `Nullable` unwrapping; null-valued ignored-type member omitted under `NullValueHandling.Include`; ignored types survive settings cloning
- [x] 1.5 Build and run the full test suite; group 1 must be green before continuing

## 2. New scalar renderings

- [x] 2.1 Add `VerifyTextWriter.WriteValue` overloads for `Int128`, `UInt128` (invariant integer text) and `Half` (invariant text through `EnsureDecimalPlace`)
- [x] 2.2 Add `ObjectWalker.WriteValue` switch cases: `Int128`, `UInt128`, `Half` (writer overloads), `BigInteger` (invariant integer via `WriteRaw`), `Memory<byte>` and `ReadOnlyMemory<byte>` (existing `byte[]` writer via `ToArray()`)
- [x] 2.3 Add `WriteJsonElement` mirroring `WriteJsonNode` (document-order objects, arrays, scrub-aware strings via `WriteString`, raw numbers, booleans, `Null`/`Undefined` as null); delegate the scalar `JsonElement` handling inside `WriteJsonNode` to it; add `JsonElement` and `JsonDocument` (root element) cases to `WriteValue`
- [x] 2.4 Update the fail-fast `VerifyException` message to name the type and point to `IgnoreMembersWithType` as the escape hatch
- [x] 2.5 Update the existing `Int128` fail-fast test to assert the new numeric rendering; keep a fail-fast test using `IntPtr` asserting the message mentions `IgnoreMembersWithType`
- [x] 2.6 Add rendering tests: `Int128`/`UInt128` min/max values; `Half` whole value gets `.0`, fractional value, `NaN`; `BigInteger` beyond `ulong` range renders as a number; `Memory<byte>`/`ReadOnlyMemory<byte>` render base64; `JsonElement` object/array/scalars render like the equivalent `JsonNode` and scrub inline Guids; `JsonDocument` renders its root element; `Int128` and `BigInteger` dictionary keys render as invariant numbers
- [x] 2.7 Build and run the full test suite; group 2 must be green before continuing

## 3. Documentation

- [x] 3.1 Document `IgnoreMembersWithType` (assignability, declared and runtime type, `Stream` default) in README.md next to `IgnoreMember`
- [x] 3.2 Document the newly supported types and the two snapshot-breaking renderings (`BigInteger`, hidden `Stream` members) in README.md
