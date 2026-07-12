# Design: Fix Code Review Findings

## Context

A code review confirmed 11 defects across the scrubbing, serialization, naming, and verification pipeline. Every finding was verified against the current source. The fixes are grouped into three waves so each wave compiles and passes tests independently, per the incremental-progress principle. The repo's own test suite disables parallelization (`TestSetup.cs`), which makes culture-mutating tests safe.

Uncommitted cosmetic edits currently in the working tree (`InnerVerifier.cs`, `SettingsTask.cs`, `Verifier.cs`) are unrelated and must be committed separately before implementation starts.

## Goals / Non-Goals

**Goals:**

- Snapshot bytes are identical regardless of machine culture (date scrubbing, dictionary ordering).
- No silent data loss: converter-backed types render meaningfully or fail fast.
- No crashes or flakiness from races, null params, non-comparable dictionary keys, or invalid file-name characters.
- Scrubbers run exactly once per verification.
- Snapshot output contains no trailing whitespace that editors would silently trim.

**Non-Goals:**

- `PrefixUnique` scoping (review finding 7) — deferred entirely by decision; no code or documentation change.
- Scrubber-pass allocation restructuring (the repeated `builder.ToString()` copies) beyond removing the double pass.
- `DateScrubber.ReplaceInner` algorithmic performance work.

## Decisions

### D1: String-date recognition uses two explicit invariant formats

`Counter.TryConvertDate` tries `"yyyy-MM-dd"` (ISO, consistent with `DateTimeParseFormat` used by the sibling methods) and then `"d"` with `CultureInfo.InvariantCulture` (equivalent to `MM/dd/yyyy`, two-digit fields mandatory). `TryConvertDateTime`/`TryConvertDateTimeOffset` replace their `null` providers with `CultureInfo.InvariantCulture` explicitly.

- Alternative considered: invariant `"d"` only (the review's minimal recommendation). Rejected because ISO date strings — the form the library itself emits elsewhere — would still never scrub.
- Consequence (breaking): `"7/12/2026"` (single-digit month, en-US short date) stops being recognized on en-US machines; `"2026-07-12"` starts being recognized everywhere.

`DateScrubber.BuildDateTimeScrubber` defaults `culture ?? CultureInfo.InvariantCulture` (was `CurrentCulture`). The `LengthCache` is already keyed by `(format, culture.Name)`, so no cache change is needed.

### D2: Dictionary entries sort by rendered key, ordinal

`WriteDictionary` projects each entry through `ConvertDictionaryKey` first, then orders with `StringComparer.Ordinal`. The existing null-value and `IsOnStack` handling operates on the projected `(renderedKey, value)` pairs.

- Fixes both confirmed problems in one move: non-comparable/mixed key types no longer hit `Comparer<object>.Default`, and string collation is culture-independent.
- Behavior change (breaking, accepted): counter tokens for Guid/DateTime keys are assigned in dictionary enumeration order (LINQ `OrderBy` fully buffers its source before sorting), and rendered tokens sort ordinally — `Guid_10` precedes `Guid_2`. Output remains deterministic per test run.

### D3: Converter-backed types get explicit cases; unknown contract-less types fail fast

`WriteValue` gains cases before the fallthrough:

- `DateOnly` → `WriteDateTime(dateOnly.ToDateTime(TimeOnly.MinValue))`. This lands in the same counter cache as `TryConvertDate`'s string path, so a `DateOnly` property and the string `"2026-07-12"` of the same date share one `DateTime_N` token.
- `TimeOnly` → raw `ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture)` (not scrubbed, consistent with `TimeSpan`).
- `Uri` → `WriteString(uri.OriginalString)`.
- `Version` → `WriteString(version.ToString())`.

`ConvertDictionaryKey` gains matching `DateOnly`/`TimeOnly` cases.

For any other type whose contract resolves to `JsonTypeInfoKind.None`, `WriteObject` throws a descriptive `VerifyException` naming the type — **except** `typeof(object)`, which legitimately renders `{}` today. The throw happens in `WriteObject` (not `BuildMembers`) so the cached member array stays a pure lookup; `BuildMembers`/`MemberCache` records the kind, or a sentinel, so `WriteObject` can distinguish "no members because contract-less" from "no members because empty type".

- Alternative considered: `ToString()` fallback for unknown `Kind.None` types. Rejected — fail fast with a descriptive message matches the project's error-handling philosophy, and silent `ToString()` could bake nondeterministic values into snapshots.

### D4: First-run initialization is a lock, not an Interlocked gate

`Verifier.AssignTargetAssembly` uses `lock` on a private static object with a `bool _assemblyAssigned` done-flag checked inside the lock, so `DirectoryReplacements.UseAssembly` completes before any concurrent verification proceeds. `DirectoryReplacements._items` becomes `volatile` (readers already snapshot it into a local).

### D5: Scrubbers run once, at document level

- `ObjectWalker.WriteString` keeps counter conversion and newline normalization but no longer runs `InstanceScrubbers`/`DirectoryReplacements` per value.
- `WriteRawWithScrubbers` collapses to plain `writer.WriteRaw` — its inputs are generated (formatted dates, `Guid.ToString("D")`), never user paths.
- `ApplyScrubbers.ApplyForPropertyValue` reduces to newline normalization (`FixNewlines`) only; per-value normalization is kept so `VerifyTextWriter.WriteString`'s multi-line detection sees `\n`-normalized text (a value starting with `\r\n` would otherwise gain an extra blank line).
- Document-level `ApplyForExtension` is the single pass for instance scrubbers and directory replacements.
- Semantics change (accepted, matches Verify): line-based scrubbers now see whole rendered lines (`Name: value`), so `ScrubLinesContaining` removes the entire property line rather than lines inside a value.

### D6: File-name sanitization uses a fixed cross-platform character set

`FileNameBuilder.Build` sanitizes the final prefix — parameter segments and `UseFileName` alike — replacing each occurrence of a fixed set (the Windows-invalid file-name characters `\ / : * ? " < > |`, control chars, plus anything from `Path.GetInvalidFileNameChars()` at runtime) with `-`.

- Fixed explicit set rather than platform-only `Path.GetInvalidFileNameChars()` so snapshot names are identical on Windows and Linux (Linux only forbids `/` and NUL).
- Replacing `*` and `?` also protects the `Directory.EnumerateFiles(directory, $"{filePrefix}.received.*")` pattern in `DeleteStaleReceivedFiles`.

### D7: Empty values emit no trailing whitespace; property names stay single-line

In `VerifyTextWriter`, the single convergence point for both contexts handles empty values:

- Pending property (object context): clear `_pendingProperty` without emitting the separator space → `Name:`.
- Array context: append comma and newline, increment `ChildCount`, skip indentation → the element contributes no whitespace of its own. Because the writer attaches each separator comma to the end of the previous element's line, a mid-array empty element's line holds only the following element's comma (`["a", "", "b"]` renders the middle element as a line containing just `,`), while a trailing empty element's line is completely blank. Neither ends in whitespace.
- `WritePropertyName` escapes `\n` (and `\r`) in names so a dictionary key containing newlines cannot break the line structure.
- The top-level `"emptyString"` sentinel in `InnerVerifier` is unaffected.
- Alternative considered: a visible sentinel like `''` for empty strings. Rejected for consistency — the property-side decision emits nothing rather than inventing a token.

### D8: Lazy enumerables are materialized once, excluding dictionary-shaped types

In `WriteMember`, a value that is `IEnumerable` but neither `ICollection` nor dictionary-shaped (not `IDictionary`, does not implement `IReadOnlyDictionary<,>`) is materialized with `Cast<object?>().ToList()`; the list serves both the `IsEmpty` check and the subsequent `WriteValue`.

- The dictionary exclusion is load-bearing: naively materializing a custom `IReadOnlyDictionary<,>`-only type into a list of KeyValuePairs would make `TryGetDictionaryEntries` stop recognizing it and render it as an array.

### D9: Minor hygiene

- `InnerVerifier.CompareAndReport` switches to `File.ReadAllTextAsync`/`WriteAllTextAsync`; all awaits in the library gain `ConfigureAwait(false)`.
- `Verifier.BuildVerifier` throws a `VerifyException` when the resolved snapshot directory does not exist; when `sourceFile` starts with `/_`, the message names `DeterministicSourcePaths`/`ContinuousIntegrationBuild` as the likely cause. README gains a note.

## Risks / Trade-offs

- [Breaking snapshot changes from D1/D2] → Called out as **BREAKING** in the proposal; consumers re-approve affected `.verified.txt` files. The repo's own verified files must be re-checked during implementation.
- [D3 fail-fast may throw for exotic types (`Half`, `Int128`) that previously rendered `{}`] → Intentional: `{}` was silent data loss. The exception message names the type and suggests handling options.
- [D5 changes line-scrubber semantics from value-scope to document-scope] → Matches Verify's documented behavior; existing tests that relied on value-scope scrubbing must be updated deliberately, not silently.
- [D7 blank-line rendering for empty array elements is ambiguous with multi-line string content] → Accepted; ambiguity is preferable to trailing whitespace that editors destroy.
- [D8 materialization changes the runtime type seen by `WriteValue`] → Safe because `WriteArray` only enumerates; dictionary-shaped types are explicitly excluded.

## Migration Plan

Implementation proceeds in three waves (see tasks.md); each wave compiles and passes the full test suite before the next begins. No deployment concerns — this is a library; consumers pick up fixes via a package update and re-approve any snapshots affected by the breaking changes above.

## Open Questions

None — all decisions were resolved during exploration.
