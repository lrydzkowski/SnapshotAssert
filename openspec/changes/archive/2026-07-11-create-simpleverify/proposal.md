# Proposal: create-simpleverify

## Why

TMHE (`R:\columbus\tmhe`) depends on the Verify snapshot-testing library across 11 test projects and ~1,700 `.verified.txt` snapshot files, but uses only a small fraction of its features. Verify carries a large dependency surface (Argon — a Json.NET fork — plus multi-framework adapters) and a ~20,500-line core that the team neither needs nor controls. SimpleVerify replaces it with a minimal, self-owned library: xUnit v3 only, System.Text.Json instead of Argon, and byte-for-byte compatible with the existing default-format snapshots so no `.verified.txt` file needs to be re-approved.

## What Changes

- Create a new NuGet library **SimpleVerify**: a single package providing snapshot verification for xUnit v3 test projects.
- Reproduce Verify's default object-graph text format byte-for-byte (quote-less indented format, `\n` newlines, `Guid_N`/`DateTime_N` counters, enums as names, declaration-order members) so existing `.verified.txt` files keep passing unchanged.
- Serialize objects using System.Text.Json contract metadata (`JsonTypeInfo`) driving a custom walker and writer; no Argon/Json.NET dependency anywhere.
- Support only the Verify API surface actually used in TMHE: `Verify(object[, VerifySettings])`, `VerifyJson(string)`, `UseParameters`, `UseFileName`, and the `VerifySettings` scrubbing/serialization methods inventoried from TMHE.
- `AddExtraSettings` accepts an STJ-shaped configuration action instead of Argon's `JsonSerializerSettings` (the one intentional API difference; call sites are ~6 small builder files).
- Keep the received/verified file workflow and DiffEngine integration (WinMerge diff-on-mismatch) unchanged.
- Ship MSBuild props that inject global usings and required assembly metadata, matching Verify's packaging behavior, so test code compiles without `using` statements.
- Out of scope: xUnit v2 and NUnit adapters, Verify plugins (Http/EF/AspNetCore/ImageSharp), `UseDirectory`/`DerivePathInfo`, strict-JSON mode, stream/binary/image snapshots, combinations API. TMHE projects adopt SimpleVerify only when they migrate to xUnit v3.

## Capabilities

### New Capabilities

- `snapshot-verification`: Core verification engine — awaitable `Verify` entry points, received/verified file lifecycle, text comparison, mismatch reporting with Verify-style exception messages, DiffEngine launch, stale received-file cleanup.
- `snapshot-serialization`: Object-graph serialization to Verify's default text format using System.Text.Json contract metadata, including null/default/empty-collection handling, member ignore rules, `AddExtraSettings`, and the `VerifyJson` string-input path.
- `snapshot-scrubbing`: Value scrubbing — `Guid_N`/`DateTime_N` counter replacement for typed values, inline GUID/date scrubbing in strings, line-based scrubbers, custom `StringBuilder` scrubbers, and opt-outs (`DontScrubGuids`, `DontScrubDateTimes`).
- `snapshot-naming`: Snapshot file naming — `{Class}.{Method}` prefix derivation, `UseParameters` parameter suffixes, `UseFileName` override, uniqueness guard against colliding prefixes.
- `xunit-v3-integration`: xUnit v3 binding and packaging — test discovery via `TestContext.Current` (class, method, theory arguments), MSBuild props with global usings, NuGet packaging with DiffEngine dependency.

### Modified Capabilities

None (greenfield project; no existing specs).

## Impact

- **New code**: this repository (`VerifyFork`) becomes the SimpleVerify library: core project, xUnit v3 glue, unit/golden-format tests, NuGet packaging.
- **Dependencies**: System.Text.Json (built-in), DiffEngine, xunit.v3.extensibility.core. No Argon, no SimpleInfoName.
- **Consumers**: TMHE.AuditLog and My.Translations.MigrationsTool (already xUnit v3, 95 snapshots combined) are the first adopters; their integration suites passing unchanged against existing snapshots is the acceptance bar. Remaining TMHE projects adopt when they migrate to xUnit v3 (separate effort).
- **Licensing**: behavior and portions of logic are ported from Verify (MIT); the MIT attribution must be carried in the repository and package.
