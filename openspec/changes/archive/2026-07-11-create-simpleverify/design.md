# Design: create-simpleverify

## Context

TMHE uses Verify in 11 test projects (~1,700 `.verified.txt` snapshots), all in Verify's default object-graph text format, always stored next to the test file. The full feature inventory (established by codebase analysis of `R:\columbus\tmhe` and `R:\external\Verify` @ v31.23.0) shows a small API surface: `Verify(object[, settings])`, `VerifyJson(string)`, `UseParameters`, `UseFileName`, and roughly ten `VerifySettings` methods. There is no global `VerifierSettings` configuration, no plugins, no `UseDirectory`, and no non-text snapshots anywhere.

Hard constraints, confirmed with the owner:

1. Existing `.verified.txt` files must keep passing unchanged (byte-for-byte format compatibility).
2. xUnit v3 only; projects on xUnit v2/NUnit adopt SimpleVerify when they migrate to v3.
3. DiffEngine stays (WinMerge diff-on-mismatch workflow).

First adopters: TMHE.AuditLog (59 snapshots) and My.Translations.MigrationsTool (36 snapshots), both already on xUnit v3 with Verify 31.16–31.19. Both projects' settings builders enable include-nulls, include-defaults, and include-empty-collections, so phase-1 snapshots exercise mostly include-everything semantics.

## Goals / Non-Goals

**Goals:**

- A single NuGet package, SimpleVerify, that AuditLog and MigrationsTool can swap in with no test-code changes beyond their `VerifySettingsBuilder` files, and no snapshot re-approval.
- System.Text.Json contract metadata replaces Argon; no Json.NET-family dependency.
- Reproduce Verify 31.x default output byte-for-byte for the inventoried feature set.
- Keep the received/verified/DiffEngine workflow identical.

**Non-Goals:**

- xUnit v2, NUnit, or any other test-framework adapter.
- Verify plugins (Http, EntityFramework, AspNetCore, ImageSharp), stream/binary/image snapshots, combinations API, strict-JSON mode, `UseDirectory`/`DerivePathInfo`, global `VerifierSettings` configuration, clipboard accept.
- General-purpose Verify replacement for the ecosystem; this library targets TMHE's usage only.

## Decisions

### D1: Single package, xUnit v3 only

Verify splits core and adapters because it supports six test frameworks. With exactly one framework in scope, a core/adapter split is speculative structure. One project, one package, containing the engine plus the ~150 lines of xUnit v3 glue. Target framework: `net10.0` — the latest BCL/System.Text.Json surface, matching the first adopters (AuditLog is net10). Consequence: consuming test projects must target net10 or later, which is acceptable since adoption is gated on xUnit v3 migration anyway. Dependencies: `xunit.v3.extensibility.core`, `DiffEngine`.

### D2: Serialization = STJ contract metadata + custom walker + custom writer

Verify's default format (unquoted names/values, no escaping) cannot be produced by `Utf8JsonWriter`, and a full `JsonSerializer.SerializeToNode` pass erases exactly what fidelity depends on: typed `Guid`/`DateTime` values (needed for `Guid_N`/`DateTime_N` counters), member ordering across inheritance, and cycle behavior.

Chosen architecture:

- **Contract**: `System.Text.Json` `DefaultJsonTypeInfoResolver` provides member discovery, naming, and getter access (`JsonTypeInfo`/`JsonPropertyInfo`). STJ attributes on DTOs work for free.
- **Walker**: a custom traversal that ports Verify's `WriteMember` semantics — null/default/empty-collection rules, member ignores, dictionary sorting, counter interception for typed scalars.
- **Writer**: a custom indented text writer producing the Verify format directly (no JSON intermediate).

Alternatives rejected:

- *Serialize to `JsonNode`, render the DOM*: loses typed-scalar identity (a `Guid` arrives as a string), STJ controls ordering and cycle handling internally, and the bool-include quirk needs per-property overrides anyway. Sentinel-tagging converters could patch around this, but the workarounds outweigh the reuse.
- *Raw reflection walker*: maximal control but reimplements contract resolution STJ already provides, and drops STJ attribute support. Also contradicts the stated goal of adopting STJ.

### D3: The byte-format contract (ported from Verify source, first-hand)

These rules come from direct reads of `VerifyJsonWriter.cs`, `SerializationSettings.cs`, and `CustomContractResolver.cs` at v31.23.0 and are the compatibility contract:

| Rule | Source |
|---|---|
| 2-space indent, `\n` newlines, `{`/`[` blocks, commas between members | Argon `Formatting.Indented` + `StringWriter.NewLine = "\n"` |
| Property names and string values unquoted, no character escaping | `QuoteName/QuoteValue = false`, `EscapeHandling.None` |
| Enums rendered as names | `StringEnumConverter` in default converter list |
| Members in declaration order, base-type members first | Json.NET `DefaultContractResolver` (alphabetical sort is opt-in and unused in TMHE) |
| Nulls omitted by default; defaults omitted by default | `NullValueHandling.Ignore` (effective), `DefaultValueHandling.Ignore` |
| `false` booleans always written despite default-ignore | `CustomContractResolver.CreateProperty` forces include for `bool` |
| Empty collections omitted by default | `SerializationSettings` (TMHE opts out via `DontIgnoreEmptyCollections`) |
| Dictionaries sorted by key | `OrderDictionaries = true` default |
| Typed `Guid` → `Guid_N`, typed `DateTime`/`DateTimeOffset` → `DateTime_N` (on by default, stable per verification) | `Counter.TryConvert` in writer overrides |
| Numeric-id scrubbing off by default | confirmed empirically (`TestCaseId: 1` in snapshots) |
| Multi-line string in property position: value starts on the next line, verbatim | `WriteValue(CharSpan)` raw-write branch |
| `byte[]` → base64; `TimeSpan` → `ToString()` | writer overrides |
| Direct self-reference → `$parentValue`; members closing a deeper reference cycle silently omitted; shared non-cyclic references serialize at each occurrence | `VerifyJsonWriter.WriteMember` + `ReferenceLoopHandling.Ignore` (stack-based, unlike STJ's `IgnoreCycles` which writes `null`) |
| Empty string target → `emptyString` file content | `InnerVerifier` string path |

Exact counter token spellings, date formats (`DateFormatter`), and edge cases are ported from Verify source and locked by golden tests rather than re-derived.

### D4: API-name compatibility for drop-in migration

Public surface keeps Verify's names — `Verifier.Verify(...)`, `VerifySettings`, `UseParameters`, `UseFileName`, all inventoried settings methods — in namespace `SimpleVerify`. NuGet-shipped `buildTransitive` props inject (when `ImplicitUsings` is enabled): `global using SimpleVerify;` and `global using static SimpleVerify.Verifier;`, mirroring Verify's packaging trick so bare `await Verify(x)` compiles.

`AddExtraSettings` keeps its name and shape but receives a SimpleVerify settings object exposing `NullValueHandling` and `DefaultValueHandling` properties with enums named identically to Argon's (`Include`/`Ignore`). Consequence: TMHE builder lambda bodies survive verbatim — migration is deleting `using Argon;` and swapping the package. `StringEnumConverter` registration (TMHE.Worker) becomes unnecessary since enum-as-name is default; the walker does not accept Json.NET converters.

### D5: Keep DiffEngine, delegate all diff behavior

DiffEngine is standalone (no Argon coupling), already handles WinMerge, build-server detection, and launch throttling (`DiffRunner.MaxInstancesToLaunch(100)` in TMHE module initializers keeps working against the same package). SimpleVerify calls `DiffRunner.LaunchAsync` on new/mismatch and `DiffRunner.Kill` on success, exactly as Verify does. No reimplementation.

### D6: Deliberate fidelity deviations (fail fast where TMHE has no usage)

Per the owner's development guidelines (fail fast, only features in use):

- **Parameterized tests without `UseParameters`**: Verify v3 auto-derives parameter suffixes from the test case. Every parameterized TMHE usage calls `UseParameters` explicitly. SimpleVerify requires it and throws a descriptive error otherwise.
- **Test attachments on failure**: not reproduced (no TMHE reliance).

Each deviation is unreachable from current TMHE code; if a later migration hits one, the error message states what to do.

### D7: Always-on directory-replacement scrubbing

Verify unconditionally replaces solution/project directory paths in rendered output with `{SolutionDirectory}`/`{ProjectDirectory}` tokens, and 6 MigrationsTool snapshots (a phase-1 adopter) contain `{SolutionDirectory}` in exception stack traces — so this scrubber is required for byte compatibility, not optional. Ported semantics:

- Pipeline order: user-registered scrubbers run first, then directory replacement, then newline normalization (matches Verify's `ApplyScrubbers` order; MigrationsTool's separator-normalizing scrubber depends on it).
- Matching is directory-separator-insensitive (`\` and `/` forms both match — MigrationsTool snapshots prove the forward-slash variant matches after their scrubber rewrites separators) and the replacement consumes the trailing separator (`{SolutionDirectory}src/...`).
- Directory values come from assembly metadata that the package's `buildTransitive` targets inject into consuming test projects at build time (extends the D4 packaging: props for usings, targets for metadata), mirroring Verify's `Verify.props`/`Verify.targets` mechanism.

### D8: Acceptance = golden-format tests + real-suite swap

Byte compatibility cannot be proven from snapshot files alone (the original objects are needed), so:

1. A golden-format unit-test suite in this repository asserts exact rendered output for every rule in D3, including cases the 95 phase-1 snapshots do not exercise (needed for later migrations).
2. The definition of done: AuditLog and MigrationsTool integration suites pass green against their existing `.verified.txt` files with only the package reference and settings-builder file changed (validated via a locally packed NuGet on a TMHE branch).

## Risks / Trade-offs

- [Unknown format edge case lurking in existing snapshots] → The swap-and-run acceptance gate catches it before publishing; golden tests are extended with each finding.
- [Json.NET member ordering (base-type-first, declaration order) differs from STJ's default property order] → The walker sorts members explicitly by declaring-type depth then metadata token order; golden tests cover inheritance hierarchies.
- [Counter/date-format subtleties (e.g. `Guid_Empty`, date precision trimming) mis-ported] → Port directly from Verify source (MIT, attribution carried) rather than re-deriving; lock with golden tests.
- [Later migrations (Verify 18–28-era snapshots) may reveal format drift across Verify versions] → Out of phase-1 scope by decision; each project migration re-runs its own suite and drift is fixed or re-approved per project at that time.
- [`AddExtraSettings` no longer accepts arbitrary Json.NET serializer mutations] → Accepted; inventory shows only null/default handling and `StringEnumConverter` are used, all covered by D4.
- [Single-consumer library: risk of over-fitting to TMHE] → Explicitly accepted; that is the stated goal.

## Migration Plan

1. Build SimpleVerify in this repository; pack a local NuGet (`0.x`).
2. On a TMHE branch: in AuditLog and MigrationsTool, replace `Verify.XunitV3` with SimpleVerify, adjust the two `VerifySettingsBuilder` files (drop `using Argon;`), run both integration suites.
3. Fix any drift in SimpleVerify (never by editing snapshots); iterate until green.
4. Publish `1.0` to the chosen feed; merge the TMHE branch.
5. Rollback strategy: revert the package reference; snapshots were never modified, so old Verify continues to pass.

## Open Questions

- Which NuGet feed distributes the package (nuget.org vs. private feed)?
- Should the repository ship a `Verify`-style `emptyString`/empty-file convention beyond what TMHE uses today, or fail fast there too? (Default: mirror Verify, cost is trivial.)
- Exact repository/license scaffolding text for the MIT attribution to VerifyTests.
