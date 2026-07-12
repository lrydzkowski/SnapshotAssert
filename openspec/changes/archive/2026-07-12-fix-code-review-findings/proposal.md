# Fix Code Review Findings

## Why

A code review of SimpleVerify confirmed 11 defects that undermine the library's core promise of deterministic, byte-compatible snapshots: culture-dependent scrubbing and sorting produce different snapshot bytes on different machines, several common .NET types silently serialize as `{}`, and a handful of edge cases (unsanitized file names, double-applied scrubbers, a first-run race) cause crashes or flaky failures. All findings were verified against the current source.

## What Changes

- **Culture-invariant date scrubbing**: all string-date parsing in `Counter` uses `CultureInfo.InvariantCulture`; string values matching `yyyy-MM-dd` or invariant `MM/dd/yyyy` are recognized; `ScrubInlineDateTimes` without an explicit culture defaults to invariant instead of `CurrentCulture`. **BREAKING** for snapshots that relied on current-culture short-date recognition (e.g. `7/12/2026` under en-US no longer scrubs; `2026-07-12` now does).
- **Deterministic dictionary ordering**: dictionary entries are sorted by their rendered key string with `StringComparer.Ordinal`, fixing crashes on non-comparable/mixed key types and culture-sensitive string collation. **BREAKING** for existing snapshots containing dictionaries with multiple Guid/DateTime keys (counter tokens are now assigned in enumeration order, and tokens sort ordinally, e.g. `Guid_10` before `Guid_2`).
- **Converter-backed types render data instead of `{}`**: explicit handling for `DateOnly` (counter-routed like `DateTime`), `TimeOnly`, `Uri`, and `Version`; any other type resolving to `JsonTypeInfoKind.None` (except `object`) fails fast with a descriptive `VerifyException`.
- **Race-free first-run initialization**: `AssignTargetAssembly` completes directory-replacement setup before any concurrent verification proceeds.
- **Single scrubbing pass**: instance scrubbers and directory replacements run exactly once per verification (document level), fixing double application of non-idempotent scrubbers.
- **Sanitized snapshot file names**: the built prefix (parameter segments and `UseFileName`) has invalid file-name characters and wildcards replaced with `-`.
- **`UseParameters(null)` guard**: throws `ArgumentNullException` with a descriptive message instead of a bare NRE.
- **Single enumeration of lazy sequences**: non-`ICollection`, non-dictionary enumerables are materialized once instead of being enumerated by both the empty check and the writer.
- **Whitespace-safe snapshot format**: empty string values produce no trailing whitespace (property renders as `Name:`, array element as a blank line); newlines in property names are escaped.
- **Async file I/O**: `CompareAndReport` uses `ReadAllTextAsync`/`WriteAllTextAsync` with `ConfigureAwait(false)`.
- **Descriptive error under CI path mapping**: a missing snapshot directory (e.g. `[CallerFilePath]` rewritten to `/_/...` by `DeterministicSourcePaths`) fails with a `VerifyException` naming the likely cause.

Explicitly out of scope: `PrefixUnique` process-lifetime scoping (review finding 7) — deferred by decision; scrubber-pass allocation restructuring beyond removing the double pass.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `snapshot-scrubbing`: string-date recognition becomes culture-invariant with defined formats; `ScrubInlineDateTimes` default culture changes to invariant; scrubbers are guaranteed to run exactly once per verification; directory-replacement initialization must complete before the first verification renders.
- `snapshot-serialization`: dictionary ordering is defined over rendered keys with ordinal comparison (including non-comparable key types); converter-backed scalar types (`DateOnly`, `TimeOnly`, `Uri`, `Version`) get defined renderings with fail-fast for unknown contract-less types; lazy enumerables are enumerated at most once; output must be free of trailing whitespace and property names must stay single-line.
- `snapshot-naming`: file-name prefixes are sanitized against invalid/wildcard characters; `UseParameters(null)` has a defined failure mode.
- `xunit-v3-integration`: a non-existent snapshot directory produces a descriptive error naming PathMap/`DeterministicSourcePaths` as the likely cause.

## Impact

- **Code**: `src/SimpleVerify/Scrubbing/Counter.cs`, `Scrubbing/DateScrubber.cs`, `Scrubbing/ApplyScrubbers.cs`, `Scrubbing/DirectoryReplacements.cs`, `Serialization/ObjectWalker.cs`, `Writing/VerifyTextWriter.cs`, `Naming/FileNameBuilder.cs`, `VerifySettings.cs`, `Verifier.cs`, `Engine/InnerVerifier.cs`.
- **Tests**: new and updated tests in `tests/SimpleVerify.Tests` mirroring the touched areas (Scrubbing, Serialization, Writing, Naming, Engine).
- **Existing snapshots**: consumers with dictionaries keyed by multiple Guids/DateTimes, or with culture-specific date strings, may need to re-approve `.verified.txt` files (called out as **BREAKING** above).
- **Dependencies**: none added or removed.
