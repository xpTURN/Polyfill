# Changelog

## [0.4.0] - 2026-10-03

### Added

- **UnscopedRefAttribute** polyfill for C# 11, **ExperimentalAttribute** and **CollectionBuilderAttribute** polyfills for C# 12, and **MemberNotNullAttribute**, **MemberNotNullWhenAttribute** and **StringSyntaxAttribute**. The compiler honors `[UnscopedRef]`, `[CollectionBuilder]` and `[Experimental]` from Roslyn 4.10 (Unity 6000.5 and later).
- `sb.AppendInterpolated($"…")` and `sb.AppendInterpolated(provider, $"…")`, which append through `AppendInterpolatedStringHandler`.
- An Editor warning that names the assembly which also declares one of this package's types publicly, when a compilation fails with CS0518 or CS0433.
- `tools/run-compat.sh` and `tests/Compat`: the version matrix check (Unity 2022.3, 6000.3 and 6000.6), with an optional player smoke and a comparison with other polyfill libraries.

### Changed

- `AppendInterpolatedStringHandler` formats integers, floating-point numbers, `decimal`, `bool`, `char`, `DateTime`, `DateTimeOffset`, `TimeSpan` and `Guid` into a stack buffer, and writes enum names from a cached table, so these holes no longer allocate (except some date and time formats inside Unity's `TryFormat`, listed in the README). Other values take the previous path.
- Each polyfill type is compiled only for target frameworks that lack it (`#if !NET5_0_OR_GREATER` and so on).
- `ModuleInitializerAttribute` is declared with `Inherited = false`, as in the BCL.
- The runtime assembly sets `noEngineReferences`.
- The tests that need Roslyn 4.10 (file-scoped types, `[UnscopedRef]`) compile on Unity 6000.5 and later. No test is reported as Inconclusive.
- The sample `XHandler` keeps a `StringBuilder` per thread instead of one shared builder, so a call nested inside a hole and aligned holes such as `{s,3}` come out right. It formats the holes through `AppendInterpolatedStringHandler`.
- The samples and the tests moved out of the package: the samples to `samples/PolyfillSample`, a Unity 2022.3 project, and the tests to `tests/com.xpturn.polyfill.tests`, which the Unity projects `tests/PolyfillTests.2022.3`, `tests/PolyfillTests.6000.3` and `tests/PolyfillTests.6000.6` list in `testables`. They reference the package by path, so Package Manager no longer offers them for import.
- The license file is the full Apache-2.0 text, `LICENSE`, and the package folder has a copy.

### Removed

- `StringBuilderExtensions.Append(StringBuilder, ref AppendInterpolatedStringHandler)`. The compiler never chose it for `sb.Append($"…")`, which calls `Append(string)`. Use `AppendInterpolated`.
- The GenericMath tests. Static abstract interface members need runtime support that Unity does not have.

A project whose `Packages/packages-lock.json` pins an earlier commit of the Git URL gets the new types after that entry is removed and Unity resolves the URL again. To stay on this release, add `#v0.4.0` to the URL.

## [0.3.1] - 2026-03-09

### Added

- **ModuleInitializerAttribute** polyfill for C# 9 / .NET 5

## [0.3.0] - 2026-02-24

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
