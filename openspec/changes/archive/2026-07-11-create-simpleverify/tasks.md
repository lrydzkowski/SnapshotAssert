# Tasks: create-simpleverify

## 1. Repository and project scaffolding

- [x] 1.1 Initialize git repository with .gitignore (bin/obj, *.received.*), MIT license with VerifyTests attribution, and README stub
- [x] 1.2 Create solution with src/SimpleVerify (net10.0, nullable enable, implicit usings) and tests/SimpleVerify.Tests (xunit.v3, net10.0)
- [x] 1.3 Add package references: xunit.v3.extensibility.core and DiffEngine in src; verify the dependency closure contains no Json.NET-family package
- [x] 1.4 Configure NuGet packaging metadata (package id SimpleVerify, license, repository URL) and confirm dotnet pack succeeds

## 2. Text writer and format core

- [x] 2.1 Implement the Verify-format text writer: 2-space indentation, \n newlines, unquoted names/values, no escaping, comma separation, {}/[] blocks
- [x] 2.2 Implement scalar rendering: enums as names, invariant-culture numbers, booleans, null literal, byte[] as base64, TimeSpan.ToString()
- [x] 2.3 Implement multi-line string rendering (value starts verbatim on the line after the property name; raw in collection position)
- [x] 2.4 Write golden-format tests for 2.1-2.3 asserting exact output strings, including the embedded pretty-printed JSON block case

## 3. Counter and scrubbing

- [x] 3.1 Port the per-verification Counter for Guid/DateTime/DateTimeOffset tokens (Guid_N, DateTime_N; stable mapping by first occurrence) from Verify source
- [x] 3.2 Port DateFormatter rendering for unscrubbed dates and wire DontScrubGuids/DontScrubDateTimes opt-outs
- [x] 3.3 Implement ScrubInlineGuids sharing the typed-Guid counter, and ScrubInlineDateTimes(format[, culture])
- [x] 3.4 Implement line scrubbers (ScrubLinesContaining case-insensitive removal, ScrubLinesWithReplace with null-removes-line) and AddScrubber whole-text scrubbers in registration order
- [x] 3.5 Implement always-on directory replacement ({SolutionDirectory}/{ProjectDirectory} tokens from assembly metadata, separator-insensitive matching, trailing separator consumed, applied after user scrubbers and before newline normalization)
- [x] 3.6 Write golden tests: repeated-value token stability, typed+inline guid sharing one token, scrubber ordering, directory replacement incl. separator-rewritten paths, scrubbers applied to all entry points

## 4. Object walker (serialization)

- [x] 4.1 Implement contract resolution via System.Text.Json DefaultJsonTypeInfoResolver with member ordering: base-type members first, declaration order within each type
- [x] 4.2 Implement traversal with default omission rules (nulls, default values except bool, empty collections) and Verify-compatible cycle handling ($parentValue for direct self-references, silent omission of members closing a deeper cycle, shared references serialized at each occurrence)
- [x] 4.3 Implement VerifySettings serialization surface: AddExtraSettings with NullValueHandling/DefaultValueHandling enums (Argon-compatible member names), DontIgnoreEmptyCollections, IgnoreMember(name)
- [x] 4.4 Implement dictionary serialization sorted by key (ordinal) and collection/nested-object traversal through the writer
- [x] 4.5 Golden tests: simple record, nested graph, inheritance ordering, include-everything TMHE builder configuration, ignored member, sorted dictionary, false-bool inclusion, $parentValue self-reference, omitted indirect cycle, shared-reference duplication

## 5. Verification engine

- [x] 5.1 Implement file lifecycle: stale received cleanup, UTF-8 \n-only received writing, verified read with CR rejection error
- [x] 5.2 Implement compare-and-report: pass path (no received file left, DiffRunner.Kill), new path (received written, New: exception with contents), mismatch path (received written, NotEqual: exception with both contents)
- [x] 5.3 Integrate DiffEngine launch on new/mismatch (delegating build-server detection and disabling to DiffEngine)
- [x] 5.4 Implement string-target verification including the emptyString convention and VerifyJson (parse via JsonNode, render through the walker/writer, descriptive parse error)
- [x] 5.5 Engine tests using temp directories covering all scenarios in the snapshot-verification spec

## 6. Naming and xUnit v3 binding

- [x] 6.1 Implement awaitable SettingsTask returned by Verify/VerifyJson supporting UseParameters/UseFileName before await
- [x] 6.2 Implement prefix derivation from TestContext.Current (class incl. nested parents, method) with descriptive no-context error, and snapshot directory from [CallerFilePath]
- [x] 6.3 Implement UseParameters segment formatting (_name=value, invariant culture, first-N pairing, too-many-values error) using parameter names from xUnit v3 metadata
- [x] 6.4 Implement the parameterized-test guard (error when theory verifies without UseParameters/UseFileName) and the unique-prefix guard
- [x] 6.5 In-process xunit.v3 tests validating naming against real TestContext, matching TMHE file-name shapes (e.g. _testCase=001)

## 7. Packaging integration

- [x] 7.1 Create buildTransitive props injecting global usings (SimpleVerify, static SimpleVerify.Verifier) when ImplicitUsings is enabled, and targets embedding solution/project directory assembly metadata for the directory-replacement scrubber
- [x] 7.2 Add a sample consumer test project referencing the packed nupkg that compiles bare `await Verify(x);` and exercises an end-to-end snapshot round-trip

## 8. TMHE acceptance validation

- [x] 8.1 Pack a local 0.x nupkg and, on a TMHE branch, swap Verify.XunitV3 for SimpleVerify in TMHE.AuditLog including its VerifySettingsBuilder (drop `using Argon;`)
- [x] 8.2 Run the AuditLog integration suite against existing .verified.txt files; fix any drift in SimpleVerify (never edit snapshots) until green
- [x] 8.3 Repeat swap and run for My.Translations.MigrationsTool (both ScrubLinesWithReplace scrubbers) until green
- [x] 8.4 Back-fill golden tests in SimpleVerify for every drift found during 8.2-8.3 and document migration steps in README
