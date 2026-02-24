# Changelog

## [0.3.0] - 2026-02-24

--

## [0.2.2] - 2026-02-24

### Added

- **DynamicallyAccessedMembersAttribute, DynamicallyAccessedMemberTypes** polyfill for C# 9 / .NET 5

## [0.2.1] - 2026-02-22

### Changed

- Disable test cases that require Mono.Cecil when it is not installed.

## [0.2.0] - 2026-02-21

### Added

- **CallerArgumentExpressionAttribute** polyfill for C# 10 / .NET 5
- **SkipLocalsInitAttribute** polyfill for C# 9 / .NET 5
- CSHARP_PREVIEW define support in Player Settings
- Regenerate Project Files (.sln, .csproj) via Editor
- Runtime test suite (Caller, ExplicitLambda, FileScopedTypes, GenericMath, Init, InterpolationNewline, ListPattern, PatternMatchSpanOr, RawStringLiteral, Record, Required, SkipLocalsInit, Switch, UnscopedRef, Utf8Literal)

---

## [0.1.0] - 2026-02-20

- First release
