# snapshot-scrubbing Delta

## ADDED Requirements

### Requirement: Date counting opt-out

`VerifySettings.DisableDateCounting()` SHALL replace every date/time value that would otherwise render as a counter token (`DateTime_N`, `DateTimeOffset_N`) with the literal token `{Scrubbed}`, matching Verify 31.x. The opt-out SHALL apply uniformly to all date scrubbing paths: typed `DateTime`, `DateTimeOffset`, and `DateOnly` members, dictionary keys, date-shaped string values, and `ScrubInlineDateTimes` matches. The `Date_MinValue` and `Date_MaxValue` special-case tokens SHALL keep their identifiers regardless of the setting. Guid counting SHALL be unaffected. Combining `DisableDateCounting()` with `DontScrubDateTimes()` SHALL NOT throw; dates then render in the literal date format as if only `DontScrubDateTimes()` were set.

#### Scenario: Distinct dates collapse to one token

- **WHEN** settings call `DisableDateCounting()` and an object with two distinct `DateTime` values and one `DateTimeOffset` value is serialized
- **THEN** all three render as `{Scrubbed}`

#### Scenario: Date-shaped string value scrubbed without counting

- **WHEN** settings call `DisableDateCounting()` and a string value `2026-07-12` is serialized
- **THEN** it renders as `{Scrubbed}`

#### Scenario: Inline date scrubbing without counting

- **WHEN** settings call `DisableDateCounting()` and `ScrubInlineDateTimes` with a format, and a string value contains a substring matching that format
- **THEN** the substring is replaced with `{Scrubbed}`

#### Scenario: Min and max dates keep special tokens

- **WHEN** settings call `DisableDateCounting()` and `DateTime.MinValue` and `DateTime.MaxValue` values are serialized
- **THEN** they render as `Date_MinValue` and `Date_MaxValue`

#### Scenario: Guid counting unaffected

- **WHEN** settings call `DisableDateCounting()` and an object with two distinct `Guid` values is serialized
- **THEN** the guids render as `Guid_1` and `Guid_2`

#### Scenario: Combined with date scrubbing disabled

- **WHEN** settings call both `DontScrubDateTimes()` and `DisableDateCounting()` and a `DateTime` property is serialized
- **THEN** no exception is thrown and the value renders in Verify's literal date format
