# Tasks: revert-empty-value-whitespace

## 1. Writer revert

- [x] 1.1 In `src/SnapshotAssert/Writing/VerifyTextWriter.cs`, change `WriteRaw` to call `BeginValue()` unconditionally (drop the `value.Length == 0` argument)
- [x] 1.2 Remove the `isEmptyValue` parameter from `BeginValue`: the pending-property branch always appends the separator space; the array branch always calls `AppendNewLineAndIndent` (delete the `builder.Append('\n')` empty-value branch)
- [x] 1.3 Confirm `EscapeName`, the multi-line `WriteString` branch, and scope handling are untouched

## 2. Test updates

- [x] 2.1 In `tests/SnapshotAssert.Tests/Writing/VerifyTextWriterTests.cs`, replace `EmptyStringPropertyValueHasNoTrailingSpace` with a Verify-compatible expectation: writing property `Name` then `""` yields `{\n  Name: \n}`
- [x] 2.2 Replace `EmptyStringBetweenArrayItemsRendersAsSeparatorOnlyLine`: `["a", "", "b"]` yields `[\n  a,\n  ,\n  b\n]`
- [x] 2.3 Replace `EmptyStringAsLastArrayItemRendersAsBlankLine`: `["a", ""]` yields `[\n  a,\n  \n]`
- [x] 2.4 Delete `EmptyValuesNeverProduceLinesEndingInWhitespace` (its guarantee is withdrawn)
- [x] 2.5 In `tests/SnapshotAssert.Tests/Serialization/ObjectWalkerTests.cs`, replace `EmptyStringRendersWithoutTrailingSpace`: `new { S = "" }` renders as `{\n  S: \n}`
- [x] 2.6 Keep `PropertyNameWithNewlinesStaysOnOneLine` and the dictionary-key escaping coverage unchanged; rename tests to describe the restored behavior

## 3. Versioning and docs

- [x] 3.1 Bump `Version` in `src/SnapshotAssert/SnapshotAssert.csproj` from 0.1.0 to 0.2.0
- [x] 3.2 Add a README note: empty string values render with a trailing separator space (Verify-compatible); recommend `.editorconfig` with `trim_trailing_whitespace = false` and `insert_final_newline = false` for `*.verified.txt` and `*.received.txt`

## 4. Verification

- [x] 4.1 Run the full test suite (`SnapshotAssert.Tests` and `SnapshotAssert.PackageConsumer`); all tests pass
- [x] 4.2 Confirm repo-owned `.verified.txt` files still pass unchanged (none contain empty-value lines)
- [x] 4.3 Spot-check against the motivating case: an object with an empty string property under include-null settings renders `ApiKey: ` byte-identical to the Verify 31.x-era snapshot
