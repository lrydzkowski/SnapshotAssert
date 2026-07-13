# Proposal: support-converter-backed-types-and-type-ignores

## Why

Verifying a graph that contains a member SnapshotAssert cannot render (e.g. `IntPtr` inside a runtime object such as a `FileStream`) fails with a `VerifyException` that offers no escape hatch short of restructuring the verified object; the original Verify library handles the same graphs. Additionally, several value-carrying types whose System.Text.Json contract resolves to `JsonTypeInfoKind.None` (`Int128`, `UInt128`, `Half`, `JsonElement`, `JsonDocument`, `Memory<byte>`, `ReadOnlyMemory<byte>`) hit the same fail-fast even though they have obvious renderings, and `BigInteger` silently renders as property soup (`{ IsPowerOfTwo: ..., IsZero: ... }`) instead of its numeric value.

## What Changes

- New `VerifySettings.IgnoreMembersWithType<T>()` and non-generic `IgnoreMembersWithType(Type type)` that omit members whose declared type or runtime value type is assignable to the given type.
- The existing hardcoded skip of `Stream`-typed members becomes a default entry in the same type-ignore mechanism, and now also matches by runtime type (a `FileStream` stored in an `object`-declared member is skipped). **BREAKING** for snapshots that currently render members whose declared type hides a `Stream` instance.
- Explicit renderings for converter-backed value types that currently fail fast: `Int128`/`UInt128` as invariant numbers, `Half` as an invariant floating-point number with the existing decimal-place normalization, `JsonElement`/`JsonDocument` as structured content (same treatment as `JsonNode`), `Memory<byte>`/`ReadOnlyMemory<byte>` as base64 (same as `byte[]`).
- `BigInteger` renders as its invariant numeric value. **BREAKING** for existing snapshots containing `BigInteger` members, which currently render as a property object.
- The fail-fast `VerifyException` for remaining contract-less types (`IntPtr`, `UIntPtr`, `Type`, reflection types, delegates) keeps its current behavior but the message now also points to `IgnoreMembersWithType` as the escape hatch.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `snapshot-serialization`:
  - New requirement: member ignoring by type (declared or runtime, assignability-based), with `Stream` ignored by default.
  - Modified requirement "Converter-backed scalar type rendering": explicit renderings for `Int128`, `UInt128`, `Half`, `JsonElement`, `JsonDocument`, `Memory<byte>`, `ReadOnlyMemory<byte>`, and `BigInteger`; fail-fast retained for the remaining contract-less types with an error message that names the type and the escape hatch.

## Impact

- `src/SnapshotAssert/Serialization/ObjectWalker.cs`: new `WriteValue` cases, `WriteJsonElement`, type-ignore check in `WriteMember`, removal of the hardcoded `Stream` check, updated fail-fast message.
- `src/SnapshotAssert/Serialization/SerializationSettings.cs`: `IgnoredMemberTypes` set with `Stream` default, cloning.
- `src/SnapshotAssert/VerifySettings.cs`: `IgnoreMembersWithType` overloads.
- `src/SnapshotAssert/Writing/VerifyTextWriter.cs`: writer overloads for the new numeric types as needed.
- `tests/SnapshotAssert.Tests`: new rendering and ignore tests; the `Int128` fail-fast test changes to assert the new rendering.
- `README.md`: document the new settings API and supported types.
- Existing consumer snapshots containing `BigInteger` members or `Stream` instances behind non-`Stream`-declared members will change on re-verification.
