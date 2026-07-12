# Tasks: Fix Code Review Findings

## 1. Preparation

- [x] 1.1 Commit the unrelated cosmetic working-tree changes (`InnerVerifier.cs`, `SettingsTask.cs`, `Verifier.cs`) separately so this change starts from a clean tree
- [x] 1.2 Run the full test suite to establish a green baseline (`dotnet test`)

## 2. Wave 1 — Culture-invariant date scrubbing (D1)

- [x] 2.1 In `Counter.TryConvertDateTime` and `Counter.TryConvertDateTimeOffset`, replace the `null` format providers with `CultureInfo.InvariantCulture`
- [x] 2.2 Rewrite `Counter.TryConvertDate` to try `"yyyy-MM-dd"` then `"d"`, both with `CultureInfo.InvariantCulture`
- [x] 2.3 In `DateScrubber.BuildDateTimeScrubber`, change the culture default from `CultureInfo.CurrentCulture` to `CultureInfo.InvariantCulture`
- [x] 2.4 Add `CounterTests`: ISO date string `2026-07-12` scrubs; invariant short date `07/12/2026` scrubs; single-digit form `7/12/2026` does not scrub
- [x] 2.5 Add a test asserting scrubbed output is identical when `CultureInfo.CurrentCulture` is temporarily set to `en-US` vs `de-DE` (restore culture in `finally`; parallelization is already disabled assembly-wide)

## 3. Wave 1 — Deterministic dictionary ordering (D2)

- [x] 3.1 In `ObjectWalker.WriteDictionary`, project entries through `ConvertDictionaryKey` first and order with `StringComparer.Ordinal`, keeping null-value and `IsOnStack` handling on the projected pairs
- [x] 3.2 Add `ObjectWalkerTests`: `Dictionary<object, string>` with mixed `int`/`string` keys serializes without throwing
- [x] 3.3 Add `ObjectWalkerTests`: keys `apple`, `Banana`, `cherry` render in ordinal order (`Banana`, `apple`, `cherry`)
- [x] 3.4 Re-check the repo's own `.verified.txt` files and existing tests for dictionary snapshots affected by the counter-token numbering change; update deliberately

## 4. Wave 1 — Converter-backed types (D3)

- [x] 4.1 Add `WriteValue` cases: `DateOnly` (via `WriteDateTime(dateOnly.ToDateTime(TimeOnly.MinValue))`), `TimeOnly` (`"HH:mm:ss.FFFFFFF"` invariant, raw), `Uri` (`OriginalString` via `WriteString`), `Version` (`ToString()` via `WriteString`)
- [x] 4.2 Add matching `DateOnly`/`TimeOnly` cases to `ConvertDictionaryKey`
- [x] 4.3 Make `WriteObject` throw a descriptive `VerifyException` naming the type when its contract resolves to `JsonTypeInfoKind.None`, except for `typeof(object)`; plumb the kind (or a sentinel) through `BuildMembers`/`MemberCache`
- [x] 4.4 Add tests: `DateOnly` scrubs to `DateTime_N` and shares a token with the equivalent ISO date string; `TimeOnly`, `Uri`, `Version` render their values; `Int128` property fails with the descriptive exception; `new object()` still renders `{}`
- [x] 4.5 Build and run the full test suite; wave 1 must be green before continuing

## 5. Wave 2 — Race-free initialization (D4)

- [x] 5.1 Replace the `Interlocked.Exchange` gate in `Verifier.AssignTargetAssembly` with a `lock` and a `bool` done-flag checked inside the lock, so `DirectoryReplacements.UseAssembly` completes before any caller proceeds
- [x] 5.2 Make `DirectoryReplacements._items` `volatile`

## 6. Wave 2 — Single scrubbing pass (D5)

- [x] 6.1 Reduce `ApplyScrubbers.ApplyForPropertyValue` to newline normalization only (drop `InstanceScrubbers` and `DirectoryReplacements`); rename to reflect its new role
- [x] 6.2 Collapse `ObjectWalker.WriteRawWithScrubbers` to plain `writer.WriteRaw`
- [x] 6.3 Confirm `ApplyForExtension` remains the single document-level pass (instance scrubbers, directory replacements, newline fix)
- [x] 6.4 Add a test with a non-idempotent scrubber (`a` → `aa`) proving exactly-once application to a string property value
- [x] 6.5 Review existing scrubber tests for reliance on per-value scoping of line scrubbers; update deliberately to document-level semantics

## 7. Wave 2 — File-name sanitization (D6)

- [x] 7.1 Add sanitization of the final prefix in `FileNameBuilder.Build`, replacing the fixed cross-platform invalid set (`\ / : * ? " < > |`, control chars, plus `Path.GetInvalidFileNameChars()`) with `-`, covering parameter segments and `UseFileName`
- [x] 7.2 Add tests: `DateTime` parameter produces a valid file name; string parameter with slashes; wildcard characters `*`/`?` replaced
- [x] 7.3 Build and run the full test suite; wave 2 must be green before continuing

## 8. Wave 3 — Guards and format integrity (D7, D8, findings 8/9)

- [x] 8.1 Add `ArgumentNullException.ThrowIfNull(parameters)` at the top of `VerifySettings.UseParameters`; add a test calling `UseParameters(null)`
- [x] 8.2 In `ObjectWalker.WriteMember`, materialize enumerables that are neither `ICollection` nor dictionary-shaped once (`Cast<object?>().ToList()`) and use the list for both the empty check and `WriteValue`
- [x] 8.3 Add tests: counting iterator enumerated exactly once; a type implementing only `IReadOnlyDictionary<,>` still renders as a dictionary
- [x] 8.4 In `VerifyTextWriter`, when the written value is empty: clear `_pendingProperty` without the separator space (property context) and skip indentation while keeping comma/newline/`ChildCount` (array context)
- [x] 8.5 Escape `\n`/`\r` in `WritePropertyName`
- [x] 8.6 Add `VerifyTextWriterTests`: `Name:` without trailing space; `["a", "", "b"]` renders a blank middle line; dictionary key with newline stays single-line; document-level assertion that no output line ends in whitespace

## 9. Wave 3 — Hygiene and CI-path guard (D9)

- [x] 9.1 Switch `InnerVerifier.CompareAndReport` to `File.ReadAllTextAsync`/`WriteAllTextAsync` and add `ConfigureAwait(false)` to all awaits in the library
- [x] 9.2 In `Verifier.BuildVerifier`, throw a `VerifyException` when the resolved snapshot directory does not exist; when `sourceFile` starts with `/_`, name `DeterministicSourcePaths`/`ContinuousIntegrationBuild` in the message
- [x] 9.3 Add a README note about the `DeterministicSourcePaths` constraint
- [x] 9.4 Add a unit test for the missing-directory error message (exercise `BuildVerifier` behavior via a path that does not exist)

## 10. Finalization

- [x] 10.1 Run the full test suite including `SimpleVerify.PackageConsumer`
- [x] 10.2 Re-approve any of the repo's own `.verified.txt` files changed by the breaking behavior (dictionary token numbering, date recognition), verifying each diff is expected
- [x] 10.3 Confirm no finding-7 (`PrefixUnique`) changes leaked in — it is explicitly out of scope
