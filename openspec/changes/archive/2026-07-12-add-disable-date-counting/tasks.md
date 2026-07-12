# Tasks: add-disable-date-counting

## 1. Settings surface

- [x] 1.1 Add private `CountDatesSetting` (`bool?`), internal `EffectiveCountDates` (defaulting to `true`), and public `DisableDateCounting()` to `VerifySettings`, following the `ScrubDateTimesSetting` pattern
- [x] 1.2 Copy `CountDatesSetting` in the `VerifySettings(VerifySettings parent)` clone constructor

## 2. Counter behavior

- [x] 2.1 Add a `countDates` constructor parameter and get-only property to `Counter`
- [x] 2.2 Short-circuit `Counter.Convert(DateTime)` to return `{Scrubbed}` when counting is disabled, after the `Date_MaxValue`/`Date_MinValue` early returns
- [x] 2.3 Short-circuit `Counter.Convert(DateTimeOffset)` the same way
- [x] 2.4 Pass `settings.EffectiveCountDates` in `InnerVerifier.CreateCounter()`

## 3. Tests

- [x] 3.1 `CounterTests`: distinct `DateTime` and `DateTimeOffset` values all convert to `{Scrubbed}` when counting is disabled; counted tokens remain the default
- [x] 3.2 `CounterTests`: `DateTime.MinValue`/`DateTime.MaxValue` keep `Date_MinValue`/`Date_MaxValue` with counting disabled; guid conversion still counts
- [x] 3.3 `VerifySettingsTests`: `DisableDateCounting()` flips `EffectiveCountDates` and the setting survives the parent-clone constructor
- [x] 3.4 Snapshot test: object with two distinct `DateTime` members, a `DateTimeOffset`, and a date-shaped string renders every date as `{Scrubbed}` under `DisableDateCounting()`
- [x] 3.5 Snapshot test: `ScrubInlineDateTimes` match renders `{Scrubbed}` under `DisableDateCounting()`
- [x] 3.6 Snapshot test: `DontScrubDateTimes()` combined with `DisableDateCounting()` does not throw and renders the literal date format

## 4. Verification

- [x] 4.1 Run the full test suite and confirm all tests pass
- [x] 4.2 Update README feature documentation if it lists supported `VerifySettings` methods
