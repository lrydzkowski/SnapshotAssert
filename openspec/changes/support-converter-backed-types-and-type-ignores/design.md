# Design: support-converter-backed-types-and-type-ignores

## Context

`ObjectWalker.WriteValue` renders a fixed set of scalar types and sends everything else that is not dictionary-shaped or enumerable to `WriteObject`, which reflects members via `DefaultJsonTypeInfoResolver`. Types whose contract resolves to `JsonTypeInfoKind.None` (other than `object`) fail fast with a `VerifyException`. Probing .NET 10 shows this bucket contains two distinct groups:

- Value-carrying types with a built-in System.Text.Json converter: `Int128`, `UInt128`, `Half`, `JsonElement`, `JsonDocument`, `Memory<byte>`, `ReadOnlyMemory<byte>`.
- Genuinely unrenderable types backed by STJ's unsupported-type converter: `IntPtr`, `UIntPtr`, `Type`, reflection types, delegates.

Separately, `BigInteger` resolves to `JsonTypeInfoKind.Object` and renders its computed properties (`IsPowerOfTwo`, `IsZero`, ...) instead of its numeric value. Member ignoring currently exists only by name (`IgnoreMember(string)`), plus a hardcoded skip of members whose declared type is `Stream`-assignable.

## Goals / Non-Goals

**Goals:**

- Render the value-carrying converter-backed types listed above, plus `BigInteger`, as meaningful scalar or structured content.
- Provide `IgnoreMembersWithType<T>()` / `IgnoreMembersWithType(Type)` as the escape hatch for members of unrenderable or unwanted types.
- Keep fail-fast behavior for the genuinely unrenderable group, with an error message that names the escape hatch.

**Non-Goals:**

- Byte-for-byte Verify 31.x parity for the newly supported types (Verify's serializer does not define a comparable rendering for most of them).
- Filtering array items or dictionary entries by type; ignoring stays member-level, consistent with `IgnoreMember(string)`.
- Renderings for `Complex`, `Rune`, `TimeZoneInfo`, or other `JsonTypeInfoKind.Object` types whose property-based rendering is deterministic today.

## Decisions

### Type ignores match by assignability, on declared and runtime type

`WriteMember` omits a member when any ignored type `T` satisfies `T.IsAssignableFrom(declaredType)` or `T.IsAssignableFrom(value.GetType())`. `Nullable<X>` declared types are unwrapped with `Nullable.GetUnderlyingType` before matching, so `IgnoreMembersWithType<IntPtr>()` also covers `IntPtr?` members.

- Exact-type matching rejected: `IgnoreMembersWithType<Stream>()` must catch `FileStream`.
- Declared-type-only matching rejected: the motivating failure was a `Stream` instance held by an `object`-declared member, which a declared-type check cannot see.

### The hardcoded Stream skip becomes a default ignored type

`SerializationSettings` gains `internal HashSet<Type> IgnoredMemberTypes` seeded with `typeof(Stream)` and copied by `Clone()`. The explicit `typeof(Stream).IsAssignableFrom(memberType)` check in `WriteMember` is deleted; the seeded entry replaces it. One mechanism instead of two, and the Stream skip gains runtime-type coverage for free. The check runs where the Stream check runs today: after the name-based ignore, before null handling, so a null-valued member of an ignored type is fully omitted rather than rendered as `null`.

### Public API mirrors `IgnoreMember`

`VerifySettings.IgnoreMembersWithType<T>()` delegates to `IgnoreMembersWithType(typeof(T))`; the non-generic overload validates against null and adds to `Serialization.IgnoredMemberTypes`. No constraint on `T`: ignoring by interface or base class is the point of assignability matching.

### New scalar renderings reuse existing writer conventions

- `Int128` / `UInt128`: new `VerifyTextWriter.WriteValue` overloads writing `ToString(CultureInfo.InvariantCulture)`, matching the `long`/`ulong` overloads. No decimal-place normalization (integers).
- `Half`: new writer overload writing `EnsureDecimalPlace(value.ToString(CultureInfo.InvariantCulture))`. Default `ToString` is shortest-round-trippable, which is what the `float`/`double` overloads' `"R"` format produces; `EnsureDecimalPlace` already passes `NaN`/`Infinity`/`-Infinity` through.
- `BigInteger`: rendered via `ToString(CultureInfo.InvariantCulture)` as an integer, no decimal-place normalization. Handled as a `WriteValue` switch case, which takes precedence over its `JsonTypeInfoKind.Object` contract.
- `Memory<byte>` / `ReadOnlyMemory<byte>`: `WriteValue` switch cases routing through the existing `byte[]` writer via `ToArray()`. The copy is acceptable for snapshot-sized data; boring beats clever.
- `JsonElement`: new `WriteJsonElement` mirroring `WriteJsonNode` — objects iterate `EnumerateObject` in document order (no key sorting, consistent with `JsonObject` handling), arrays iterate `EnumerateArray`, strings route through `WriteString` so scrubbing and counters apply, numbers write `GetRawText()`, booleans and null map directly, `Undefined` renders as `null`. The scalar `JsonElement` handling currently inlined in `WriteJsonNode`'s `JsonValue` branch delegates to `WriteJsonElement` to avoid duplication.
- `JsonDocument`: `WriteValue` case delegating to `WriteJsonElement(document.RootElement)`.

### Dictionary keys need no change

`ConvertDictionaryKey` falls back to `Convert.ToString(key, CultureInfo.InvariantCulture)`, which routes through `IFormattable` for `Int128`, `UInt128`, `Half`, and `BigInteger`, producing the same invariant text as the value rendering.

### Fail-fast message names the escape hatch

The `VerifyException` for remaining `JsonTypeInfoKind.None` types keeps its current first sentence and replaces the generic advice with: convert the value to a supported type or ignore the member with `VerifySettings.IgnoreMembersWithType<T>()`.

## Risks / Trade-offs

- **BREAKING** `BigInteger` snapshot rendering changes from a property object to a number → called out in proposal and release notes; affected snapshots must be re-approved.
- **BREAKING** members whose declared type hides a `Stream` instance are now omitted → same treatment; this aligns behavior with the intent of the existing Stream skip.
- Broad ignored types (e.g. `IgnoreMembersWithType<object>()`) silently drop every member → accepted; assignability semantics are documented in README, and the same footgun exists in Verify.
- Runtime-type check costs a `GetType()` and set scan per member → negligible against the existing reflection-based member enumeration; the set is almost always a single `Stream` entry.
- `Memory<T>` for non-`byte` element types remains unsupported and keeps the fail-fast → acceptable; no known consumer scenario, and the escape hatch now exists.

## Migration Plan

Ship as a minor version with release notes flagging the two snapshot-breaking renderings. Consumers with affected snapshots re-run verification and re-approve. No rollback complexity: the package is additive apart from the rendering changes.

## Open Questions

None.
