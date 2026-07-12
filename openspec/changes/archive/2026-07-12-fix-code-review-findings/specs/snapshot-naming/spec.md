# snapshot-naming Delta

## ADDED Requirements

### Requirement: File-name prefix sanitization

The library SHALL sanitize the final snapshot file-name prefix — covering both `UseParameters` value segments and `UseFileName` values — by replacing every character that is invalid in file names on Windows or Linux (`\`, `/`, `:`, `*`, `?`, `"`, `<`, `>`, `|`, control characters, and any character reported by `Path.GetInvalidFileNameChars()` at runtime) with `-`. Sanitization SHALL produce the same file name on every operating system, and sanitized prefixes SHALL never contain wildcard characters that could corrupt the stale-received-file cleanup pattern.

#### Scenario: DateTime parameter value

- **WHEN** a verification calls `UseParameters(new DateTime(2026, 7, 12))`
- **THEN** the resulting file name contains no `/` or `:` characters and the files are written successfully

#### Scenario: Parameter value containing wildcards

- **WHEN** a verification calls `UseParameters("a*b?c")`
- **THEN** the wildcards are replaced with `-` in the file name and stale-file cleanup only matches this verification's own received files

#### Scenario: UseFileName with invalid characters

- **WHEN** a verification calls `UseFileName("reports/2026:v1")`
- **THEN** the snapshot files are created in the snapshot directory with the invalid characters replaced by `-`

## MODIFIED Requirements

### Requirement: UseParameters appends parameter segments

`UseParameters(params object?[] values)` SHALL append `_{parameterName}={value}` segments to the file-name prefix, pairing the given values with the test method's parameter names in declaration order. When fewer values than parameters are supplied, the first N parameter names SHALL be used; supplying more values than parameters SHALL fail with a descriptive error. Passing `null` as the params array itself SHALL fail with an `ArgumentNullException` identifying the `parameters` argument rather than an unexplained `NullReferenceException`. Value formatting SHALL match Verify's (invariant culture).

#### Scenario: Single parameter segment

- **WHEN** a theory with parameter `testCase` awaits `Verify(result, settings).UseParameters(testCase.TestCaseId.ToString("D3"))`
- **THEN** the file-name prefix ends with `_testCase=001` for test case id 1

#### Scenario: Too many parameter values

- **WHEN** `UseParameters` receives more values than the test method has parameters
- **THEN** the verification fails with an error describing the mismatch

#### Scenario: Null params array

- **WHEN** a test calls `UseParameters(null)` binding `null` to the params array itself
- **THEN** an `ArgumentNullException` naming the `parameters` argument is thrown
