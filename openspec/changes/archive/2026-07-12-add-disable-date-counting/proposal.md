# Proposal: add-disable-date-counting

## Why

Repeated captures of the current time (e.g. several `DateTime.Now` calls in quick succession) may produce one, two, or three distinct values depending on timer resolution, so the counted tokens (`DateTime_1`, `DateTime_2`, …) vary between test runs and make snapshots flaky. The original Verify library solves this with `VerifySettings.DisableDateCounting()`, which SnapshotAssert does not yet offer.

## What Changes

- Add `VerifySettings.DisableDateCounting()`. When called, every scrubbed date/time value is replaced with the literal token `{Scrubbed}` instead of a counted moniker, matching Verify 31.x behavior.
- The setting flows through the single `Counter` choke point, so it applies uniformly to typed `DateTime`, `DateTimeOffset`, and `DateOnly` members, dictionary keys, date-shaped string values, and `ScrubInlineDateTimes` matches.
- `Date_MinValue` / `Date_MaxValue` special-case tokens keep their identifiers regardless of the setting, matching Verify.
- Guid counting is unaffected.
- The method is exposed only on `VerifySettings` — no `SettingsTask` fluent forwarding and no global settings mechanism.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `snapshot-scrubbing`: the "Typed date counter scrubbing" behavior gains an opt-out of counting — `DisableDateCounting()` replaces all date tokens with `{Scrubbed}` while leaving guid counting and min/max special cases untouched.

## Impact

- `src/SnapshotAssert/VerifySettings.cs`: new public method and setting, copied in the parent-clone constructor.
- `src/SnapshotAssert/Scrubbing/Counter.cs`: short-circuit in the date `Convert` methods when counting is disabled.
- `src/SnapshotAssert/Engine/InnerVerifier.cs`: thread the setting into the `Counter` constructor.
- Tests: `CounterTests`, `VerifySettingsTests`, and an end-to-end snapshot test.
- No breaking changes; default behavior is unchanged.
