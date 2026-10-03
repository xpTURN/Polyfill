# Compatibility

Measurements behind the [README](../README.md). [`tools/run-compat.sh`](../tools/run-compat.sh) writes the tables between the `compat:` comments (`--update-docs`); the text around them is edited by hand.

```sh
tools/run-compat.sh                                         # gate: probe and package tests in the three editors
tools/run-compat.sh --smoke --net-framework                 # also Mono and IL2CPP players, and the .NET Framework profile
tools/run-compat.sh --player-tests                          # also the package tests in IL2CPP players, one editor after another
tools/run-compat.sh --no-gate --alternatives --update-docs  # re-measure the other libraries and rewrite the tables below
```

## Case studies

Measured in October 2026. The compiler errors were reproduced in Unity 2022.3.62f3, 6000.3.9f1 and 6000.6.0f1. The library rows come from .NET Standard 2.1 projects built with the .NET SDK, with and without this package's Runtime sources. Embedded types were read from the metadata of each package's `netstandard2.1` assembly. ZLogger, VitalRouter, XLogger, XString and Klotho are also used with this package inside Unity. MemoryPack was only checked outside Unity.

## Version matrix

The probe is one source file with a line per type-dependent feature, compiled as its own asmdef with `-langversion:preview` ([Probe.cs](../tests/Compat/Assets/Cases/C1_Polyfill/Probe.cs)): `init`, `record`, `readonly record struct`, `required`, `[SetsRequiredMembers]`, `[InterpolatedStringHandler]`, `[InterpolatedStringHandlerArgument]`, `[CallerArgumentExpression]`, `[SkipLocalsInit]`, `[ModuleInitializer]`, `[DynamicallyAccessedMembers]` and `[UnscopedRef]`. Roslyn 4.3 ignores `[UnscopedRef]`, so that line is compiled from Unity 6000.5 only: the maximum is 11 before it and 12 from it. The gate is the probe assembly existing, not the absence of error lines, because Bee stops after the first assembly that fails.

The package tests run in the editor on the PlayMode platform, once with each API compatibility level, and once in an IL2CPP player. The smoke runs in a Mono and an IL2CPP player for macOS: records and `with`, `required`, a custom handler, `AppendInterpolated`, `[CallerArgumentExpression]`, `[SkipLocalsInit]`, a module initializer, the nullable attributes, attributes read through reflection, and the syntax that needs no type. From 6000.5 it adds `file` types, `[UnscopedRef]`, collection expressions with `[CollectionBuilder]`, primary constructors and `[Experimental]` ([Smoke.cs](../tests/Compat/Assets/Smoke/Smoke.cs)).

<!-- compat:matrix -->
| Check | 2022.3.62f3 | 6000.3.9f1 | 6000.6.0f1 |
|---|---|---|---|
| Probe: type-dependent features | 11 / 11 | 11 / 11 | 12 / 12 |
| Package tests: editor, .NET Standard 2.1 | 111 / 111 | 111 / 111 | 124 / 124 |
| Package tests: editor, .NET Framework | 111 / 111 | 111 / 111 | 124 / 124 |
| Package tests: IL2CPP player | 111 / 111 | 111 / 111 | 124 / 124 |
| Smoke: Mono player | 12 / 12 | 12 / 12 | 18 / 18 |
| Smoke: IL2CPP player | 12 / 12 | 12 / 12 | 18 / 18 |
| Measured | 2026-10-03 | 2026-10-03 | 2026-10-03 |
<!-- /compat:matrix -->

## Other polyfill libraries

| Option | Form | Sets language version |
|--------|------|-----------------------|
| xpTURN.Polyfill | UPM, one shared assembly, public types | Yes: build, packages, IDE |
| [PolySharp](https://github.com/Sergio0694/PolySharp) | NuGet source generator, internal types per assembly | No |
| [PolyfillLib](https://github.com/SimonCropp/Polyfill) | NuGet DLL with 2 dependency DLLs, public types | No |
| [Polyfill](https://github.com/SimonCropp/Polyfill) (source-only) | NuGet source files | No |
| [Meziantou.Polyfill](https://github.com/meziantou/Meziantou.Polyfill) | NuGet source generator | No |
| [ModernCSharpForUnity](https://github.com/iAcolyte/ModernCSharpForUnity) | UPM source generator + Project Settings page | Yes: `csc.rsp` per asmdef in `Assets`, C# 9 to 12 |
| [IsExternalInit](https://github.com/CorundumGames/IsExternalInit) (Corundum) | OpenUPM DLL, public type | No |

Each cell counts the probe features that compile with that option. The probe is compiled by the editor's own compiler with the response file Unity generated for the probe asmdef, with the package reference replaced by the option's DLL, generator or source files. The libraries are downloaded when the script runs and are not imported as assets. An option whose assembly fails outside the probe, for example on a generator's own output, counts 0, and the cell names the first error codes. The last two rows reference this package together with another provider.

<!-- compat:alternatives -->
| Option | 2022.3.62f3 | 6000.3.9f1 | 6000.6.0f1 |
|---|---|---|---|
| No polyfill | 0 / 11, CS0246, CS0518 | 0 / 11, CS0246, CS0518 | 0 / 12, CS0246, CS0518 |
| xpTURN.Polyfill (this repository) | 11 / 11 | 11 / 11 | 12 / 12 |
| PolySharp 1.15.0 | 10 / 11, CS0246, CS0103 | 10 / 11, CS0246, CS0103 | 11 / 12, CS0122 |
| PolySharp 1.16.0 | 10 / 11, CS0246, CS0103 | 10 / 11, CS0246, CS0103 | 11 / 12, CS0122 |
| PolySharp 1.16.0, runtime-supported attributes on | 0 / 11, CS8336 | 0 / 11, CS8336 | 0 / 12, CS8336 |
| PolySharp 1.16.0, runtime-supported attributes on, embedded attribute off | 11 / 11 | 11 / 11 | 12 / 12 |
| PolyfillLib 11.4.1 | 11 / 11 | 11 / 11 | 12 / 12 |
| Polyfill 11.4.1 (source-only) | 0 / 11, CS0106, CS1001 | 0 / 11, CS0106, CS1001 | 0 / 12, CS0106, CS1001 |
| Meziantou.Polyfill 1.0.165 | 0 / 11, CS0101, CS0111 | 0 / 11, CS0101, CS0111 | 0 / 12, CS8336 |
| ModernCSharpForUnity 0.1.0 | 0 / 11, CS0246, CS0518, CS9057, package requires Unity 6000.0 | 0 / 11, CS0246, CS0518, CS9057 | 9 / 12, CS0246, CS0122 |
| IsExternalInit (Corundum) 1.0.0 | 3 / 11, CS0246, CS0656 | 3 / 11, CS0246, CS0656 | 3 / 12, CS0246, CS0656 |
| xpTURN.Polyfill + IsExternalInit DLL | 8 / 11, CS0518 | 8 / 11, CS0518 | 9 / 12, CS0518 |
| xpTURN.Polyfill + PolySharp 1.16.0, both settings | 11 / 11 | 11 / 11 | 12 / 12 |
| Measured | 2026-10-03 | 2026-10-03 | 2026-10-03 |
<!-- /compat:alternatives -->

- **Runtime-supported attributes**: by default PolySharp leaves out the attributes that .NET 5 and later define, `[DynamicallyAccessedMembers]` among them. Unity's profile has no public copy, so that line fails (CS0246 on Roslyn 4.3, CS0122 on Roslyn 4.10).
- **CS8336**: PolySharp 1.16.0 with `PolySharpIncludeRuntimeSupportedAttributes`, and Meziantou.Polyfill on Roslyn 4.10, emit `Microsoft.CodeAnalysis.EmbeddedAttribute`. Unity's compilers reserve that name, so the whole assembly fails. PolySharp stops emitting it with `PolySharpUseEmbeddedAttributeForGeneratedTypes = false`.
- **No MSBuild**: those PolySharp options are MSBuild properties. In Unity they must be written as a `.globalconfig` file beside the asmdef ([runtime-supported attributes on](../tests/Compat/Alternatives/polysharp-runtime.globalconfig), [both options](../tests/Compat/Alternatives/polysharp.globalconfig)).
- **Duplicate definitions**: with its default settings, Meziantou.Polyfill fails on Roslyn 4.3 with CS0101 and CS0111.
- **CS9057**: the ModernCSharpForUnity generator is built against Roslyn 4.8. Roslyn 4.3 does not load it. The package itself declares Unity 6000.0.
- **C# 14 source**: the source-only Polyfill package uses `extension` blocks, which neither Roslyn version parses (CS0106, CS1001).
