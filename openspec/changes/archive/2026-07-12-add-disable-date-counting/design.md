# Design: add-disable-date-counting

## Context

Date scrubbing currently always counts: `Counter` (`src/SnapshotAssert/Scrubbing/Counter.cs`) caches each unique `DateTime`/`DateTimeOffset` and assigns `DateTime_{n}` / `DateTimeOffset_{n}` tokens. Every date path funnels through this one class: typed members and dictionary keys via `ObjectWalker`, date-shaped strings via `Counter.TryConvert(ReadOnlySpan<char>)`, and `ScrubInlineDateTimes` via `DateScrubber`. `InnerVerifier.CreateCounter()` constructs the `Counter` from `VerifySettings.EffectiveScrubDateTimes` and `EffectiveScrubGuids`.

Verify 31.x offers `DisableDateCounting()`, which replaces all date tokens with the literal `{Scrubbed}` (verified against Verify's `docs/dates.md` and `Counter_Date.cs`). Its `Date_MinValue`/`Date_MaxValue` special cases are evaluated before the counting check, so they keep their identifiers.

## Goals / Non-Goals

**Goals:**

- `VerifySettings.DisableDateCounting()` producing `{Scrubbed}` for every scrubbed date/time value, byte-compatible with Verify 31.x snapshots.
- Uniform effect across all date paths (typed members, dictionary keys, string recognition, inline scrubbing) without touching each path individually.
- Default behavior unchanged.

**Non-Goals:**

- No `SettingsTask` fluent forwarding (the fluent surface intentionally exposes only `UseParameters`/`UseFileName`).
- No global settings mechanism (`VerifierSettings`-style statics do not exist in this library).
- No change to guid counting.

## Decisions

### Replacement token is the literal `{Scrubbed}`

Matches Verify 31.x exactly, keeping `.verified.txt` files portable between libraries. A style-consistent alternative (`DateTime_Scrubbed`) was rejected because it would diverge from Verify snapshots for no functional gain.

### Flag flows as a `Counter` constructor argument

`Counter` gains a `countDates` constructor parameter alongside the existing `scrubDateTimes`/`scrubGuids`, exposed as a get-only property, and `InnerVerifier.CreateCounter()` passes `settings.EffectiveCountDates`. This matches the existing immutable-per-verification pattern; a mutable property on `Counter` was rejected as inconsistent with it.

### Short-circuit inside `Counter.Convert`, after the min/max checks

`Convert(DateTime)` and `Convert(DateTimeOffset)` return `{Scrubbed}` before the cache lookup when counting is disabled. The `Date_MaxValue`/`Date_MinValue` early returns stay above the short-circuit, matching Verify. Because every date path calls these methods, `ObjectWalker`, string recognition, and `DateScrubber` need no changes.

### `VerifySettings` follows the existing nullable-setting pattern

Private `CountDatesSetting` (`bool?`), internal `EffectiveCountDates => CountDatesSetting ?? true`, public `DisableDateCounting()` setting it to `false`, and a copy in the parent-clone constructor — mirroring `ScrubDateTimesSetting`.

### Combining with `DontScrubDateTimes()` is a silent no-op

Unlike `DontScrubGuids()` + `ScrubInlineGuids()` (contradictory, guarded), disabling scrubbing merely makes counting moot: dates render literally and `Convert` is never reached. Verify does not guard this combination either.

## Risks / Trade-offs

- [Information loss: two distinct timestamps both render `{Scrubbed}`, so a regression swapping one date for another becomes invisible] → Inherent to the feature and the reason it is opt-in; the default keeps counted tokens.
- [Token style `{Scrubbed}` differs from the `DateTime_N` family] → Intentional Verify parity; documenting the token in the spec prevents future "cleanup" to a counted style.
