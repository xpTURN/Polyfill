# xpTURN.Polyfill

**English** | [한국어](README.ko-KR.md) | [日本語](README.ja-JP.md) | [简体中文](README.zh-CN.md)

One package that lets a Unity project compile C# 9 to 11 code (`record`, `init`, `required`, custom interpolated string handlers, module initializers), the C# 12 attributes on Unity 6000.5 and later, and the open-source libraries written around that syntax.

Unity compiles against .NET Standard 2.1 and defaults to C# 9.0, so two things are missing. This package supplies both:

- **Runtime**: the BCL types the compiler looks for (`IsExternalInit`, `RequiredMemberAttribute`, `InterpolatedStringHandlerAttribute`, …), declared once and publicly for every assembly in the project.
- **Editor**: one menu command that applies `-langversion:preview` to the Player build and to the generated `.csproj` files, so Unity and the IDE agree.

## Why you need it

### Unity leaves the gap to you

The [Unity manual](https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html) sets the language version to C# 9.0, lists *init only setters* and *module initializers* as unsupported, and gives this advice for records:

> Users can work around this issue by declaring the `System.Runtime.CompilerServices.IsExternalInit` type in their own projects.

Without the types the build stops at compile time:

| You write | Compiler error without the type |
|-----------|---------------------------------|
| `init`, `record`, `readonly record struct` | CS0518: Predefined type `System.Runtime.CompilerServices.IsExternalInit` is not defined or imported |
| `required` | CS0656: Missing compiler required member `RequiredMemberAttribute..ctor` (and `CompilerFeatureRequiredAttribute..ctor`) |
| `[InterpolatedStringHandler]`, `[CallerArgumentExpression]`, `[ModuleInitializer]`, `[SkipLocalsInit]`, `[DynamicallyAccessedMembers]` | CS0246: The type or namespace name could not be found (Unity 6000.6 reports CS0122, inaccessible due to its protection level, for `[CallerArgumentExpression]` and `[DynamicallyAccessedMembers]`) |

Raw string literals, list patterns, UTF-8 literals and file-scoped namespaces need no type, only the language version, which the Editor command sets. Keep MonoBehaviour classes out of file-scoped namespaces, though ([why](#monobehaviour-and-namespace-in-unity)). `file` types need no type either, but the compiler in Unity 6000.3 and earlier rejects them (CS0116). They compile in Unity 6000.6.

### Libraries bring their own copy, but only for themselves

Popular libraries already use this syntax. They compile because each one embeds a private copy of the same types in its .NET Standard 2.1 assembly:

| NuGet package | Support types embedded (of 12 checked) | Visibility |
|---------------|------------------------|------------|
| ZLogger 2.5.10 | 12 | internal |
| Utf8StringInterpolation 1.3.1 | 11 | internal |
| R3 1.3.0 | 11 | internal |
| ZLinq 1.5.4 | 11 | internal |
| MemoryPack.Core 1.21.4 | 1 (`IsExternalInit`) | internal |
| VitalRouter 2.2.0 | 0 | n/a |
| ZString 2.6.0 | 0 | n/a |

An internal copy lets the library compile. It does nothing for your assembly. When your code declares a record command, a `required` member or its own handler, the compiler looks for the type again and does not find it.

## Case studies

### Using open-source libraries

Each row is code from the library's own README, compiled against its NuGet package.

| Library | Code from its README | Without this package | With this package |
|---------|----------------------|----------------------|-------------------|
| [ZLogger](https://github.com/Cysharp/ZLogger) 2.5.10 | `logger.ZLogInformation($"frame={frame} t={t:F2}")` | CS8773 at C# 9.0: *interpolated string handlers* is not available | 0 errors from C# 10 |
| [VitalRouter](https://github.com/hadashiA/VitalRouter) 2.2.0 | `public readonly record struct MoveCommand(...) : ICommand;` | CS0518 | 0 errors |
| [MemoryPack](https://github.com/Cysharp/MemoryPack) 1.21.4 | `[MemoryPackable]` class with `required … { get; init; }`, `partial record` | CS0518, CS0656 ×2 | 0 errors |

- **ZLogger** needs only the language version. Its Unity guide asks for a `csc.rsp` beside every asmdef that references ZLogger, plus `LangVersion.props` and CsprojModifier for the IDE. The Editor command here replaces all three.
- **VitalRouter** recommends `readonly record struct` commands and ships no `IsExternalInit`, so the recommended form does not compile until the type exists.
- **MemoryPack** models use `required` and `init`, and its generator emits `scoped ref` (C# 11) into your assembly. Both halves of this package are needed.

### Building on top of a library

- [xpTURN.XLogger](https://github.com/xpTURN/XLogger) wraps ZLogger with `XLog*` methods that can be stripped from release builds. It declares 7 handlers with `[InterpolatedStringHandler]`, and mirrors ZLogger's `AppendFormatted` signature with `[CallerArgumentExpression]` and `[DynamicallyAccessedMembers]`. All three types come from this package.
- [xpTURN.XString](https://github.com/xpTURN/XString) adds `XString.Format($"...")` over ZString's `Utf16ValueStringBuilder` with its own handler.

### Replacing a dependency

[xpTURN.Klotho](https://github.com/xpTURN/Klotho) removed ZLogger together with its dependency graph, 13 NuGet DLLs in total. Its own logger keeps the `$"..."` call style through six `ref struct` handlers: more than 500 interpolated log calls across 65 files. The logging assembly references `xpTURN.Polyfill.Runtime` and nothing else.

### Source generators and module initializers

The Klotho source generator emits `[ModuleInitializer]` registrars for components, data assets, and the commands and messages declared in other assemblies, so game types register themselves without a hand-written list. Unity lists module initializers as unsupported, and MemoryPack's README tells Unity users to register union formatters by hand. Klotho takes `ModuleInitializerAttribute` from this package and runs the module constructors itself with `RuntimeHelpers.RunModuleConstructor` before its registries are first read.

### Before and after in our own projects

| | Before (in-house client, January 2026) | After (sample with the same VContainer + VitalRouter + ZLogger stack) |
|-|----------------------------------------|-----------------------------------------------------------------------|
| Language version for the build | 6 `csc.rsp` files, one per asmdef folder | 0 `csc.rsp` files |
| Language version for the IDE | `LangVersion.props` + `Directory.Build.props` + CsprojModifier | automatic |
| Support types | 4 hand-copied files in a project-local asmdef | package reference |
| Setup | repeated for each new asmdef | 1 package, 1 menu command |

Nine of our Unity projects now reference the package, on Unity 2022.3.62f3, 6000.3.9f1 and 6000.6.0f1.

### When you do not need it

- Your code stays within C# 9 without `init` or `record`, and no library in the project requires newer syntax at the call site.
- A single asmdef needs a newer language version and none of the types: a `csc.rsp` beside that asmdef is enough.
- You need a feature that depends on the runtime, such as static abstract interface members. No polyfill can add those.

> How these were measured: [docs/Compatibility.md](./docs/Compatibility.md#case-studies).

## Compared with other options

Polyfill libraries written for .NET projects assume a recent compiler and MSBuild properties. Unity has neither: it pins its own Roslyn (4.3 in 2022.3 and 6000.3, 4.10 in 6000.6) and builds without MSBuild. So the same library can behave differently inside Unity.

The last column says which of 12 type-dependent features compile in Unity 2022.3, 6000.3 and 6000.6 with `-langversion:preview`: `init`, `record`, `readonly record struct`, `required`, `[SetsRequiredMembers]`, `[InterpolatedStringHandler]`, `[InterpolatedStringHandlerArgument]`, `[CallerArgumentExpression]`, `[SkipLocalsInit]`, `[ModuleInitializer]`, `[DynamicallyAccessedMembers]` and `[UnscopedRef]` (only on 6000.6, because Roslyn 4.3 ignores it). Per-editor counts, how they are measured and why the generators fail: [docs/Compatibility.md](./docs/Compatibility.md#other-polyfill-libraries).

| Option | Form | Sets language version | Features that compile |
|--------|------|-----------------------|-----------------------|
| No polyfill | n/a | n/a | none |
| **xpTURN.Polyfill 0.4.0** | UPM, one shared assembly, public types | Yes: build, packages, IDE | all |
| [PolySharp](https://github.com/Sergio0694/PolySharp) 1.15.0, 1.16.0 | NuGet source generator, internal types per assembly | No | all but `[DynamicallyAccessedMembers]` |
| PolySharp 1.16.0 with `PolySharpIncludeRuntimeSupportedAttributes` | same | No | none (CS8336), or all once a `.globalconfig` also turns the embedded attribute off |
| [PolyfillLib](https://github.com/SimonCropp/Polyfill) 11.4.1 | NuGet DLL with 2 dependency DLLs, public types | No | all |
| [Polyfill](https://github.com/SimonCropp/Polyfill) 11.4.1 (source-only) | NuGet source files | No | none: C# 14 syntax |
| [Meziantou.Polyfill](https://github.com/meziantou/Meziantou.Polyfill) 1.0.165 | NuGet source generator | No | none: duplicate definitions, CS8336 on 6000.6 |
| [ModernCSharpForUnity](https://github.com/iAcolyte/ModernCSharpForUnity) 0.1.0 | UPM source generator + Project Settings page | Yes: `csc.rsp` per asmdef in `Assets`, C# 9 to 12 | 9 of 12 on 6000.6 only. Roslyn 4.3 does not load the generator (CS9057), and the package declares Unity 6000.0 |
| [IsExternalInit](https://github.com/CorundumGames/IsExternalInit) 1.0.0 | OpenUPM DLL, public type | No | `init`, `record` and `readonly record struct` |

This package is plain C# source with no generator, so it does not depend on the Roslyn version. That is why one setup covers 2022.3 through 6000.6.

Where the others are stronger:

- Generators give every assembly its own `internal` copy. No asmdef reference is needed and two copies never clash.
- PolySharp and PolyfillLib cover more types, such as the trimming annotations (`RequiresUnreferencedCode`, `DynamicDependency`), and PolyfillLib also adds BCL methods that .NET Standard 2.1 lacks. Unity's compiler and IDE do nothing with the trimming annotations, so this package leaves them out.
- ModernCSharpForUnity lets you pick the language version and nullable setting per project. This package applies `preview` only.

Limits of this package to know before choosing it:

- Types are `public` in a shared assembly, so each asmdef that uses them needs a reference to `xpTURN.Polyfill.Runtime`.
- Do not reference a second assembly that exposes the same types publicly. With this package and a DLL that also declares a public `IsExternalInit`, `init` fails with CS0518. The Editor then names that assembly in a warning.
- It compiles side by side with PolySharp 1.16.0 when the `.globalconfig` above sets both options.
- No polyfill adds what the runtime lacks: static abstract interface members (generic math), `ref` fields and inline arrays.

## Provided types

| C# version | Feature | Types |
|------------|----------|--------|
| C# 9  | `init`-only setter | [IsExternalInit](./src/Polyfill/Assets/Polyfill/Runtime/Init/IsExternalInit.cs) |
| C# 9  | Module initializer | [ModuleInitializerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Module/ModuleInitializerAttribute.cs) |
| C# 9  | Skip locals init | [SkipLocalsInitAttribute](./src/Polyfill/Assets/Polyfill/Runtime/SkipLocalsInit/SkipLocalsInitAttribute.cs) |
| C# 9  | Dynamically accessed members (trimmer analysis) | [DynamicallyAccessedMembersAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/DynamicallyAccessedMembersAttribute.cs), [DynamicallyAccessedMemberTypes](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/DynamicallyAccessedMemberTypes.cs) |
| C# 9  | Nullable analysis of members set by a method | [MemberNotNullAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/MemberNotNullAttribute.cs), [MemberNotNullWhenAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/MemberNotNullWhenAttribute.cs) |
| C# 10 | Custom interpolated string handler | [InterpolatedStringHandlerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerAttribute.cs), [InterpolatedStringHandlerArgumentAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerArgumentAttribute.cs) |
| C# 10 | Caller expression argument | [CallerArgumentExpressionAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Caller/CallerArgumentExpressionAttribute.cs) |
| C# 11 | `required` member | [RequiredMemberAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/RequiredMemberAttribute.cs) |
| C# 11 | Constructor satisfying `required` | [SetsRequiredMembersAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/SetsRequiredMembersAttribute.cs) |
| C# 11 | Compiler feature requirement | [CompilerFeatureRequiredAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/CompilerFeatureRequiredAttribute.cs) |
| C# 11 | `ref` to a struct's own field (Unity 6000.5 and later; earlier compilers ignore it) | [UnscopedRefAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/UnscopedRefAttribute.cs) |
| C# 12 | Collection expressions for your own types (Unity 6000.5 and later) | [CollectionBuilderAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Collections/CollectionBuilderAttribute.cs) |
| C# 12 | Diagnostics for experimental APIs (Unity 6000.5 and later) | [ExperimentalAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/ExperimentalAttribute.cs) |
| IDE   | Language of a string argument (regex, JSON, date format) | [StringSyntaxAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/StringSyntaxAttribute.cs) |

Each type is compiled only when the target framework lacks it (`#if !NET5_0_OR_GREATER` and so on), so it gives way to the runtime's own type.

### StringBuilder helper

`sb.AppendInterpolated($"…")` appends through `AppendInterpolatedStringHandler` instead of building the string first:

```csharp
using System.Globalization;
using System.Text;

var sb = new StringBuilder();
sb.AppendInterpolated($"frame={frame} t={t:F2} state={state}");
sb.AppendInterpolated(CultureInfo.InvariantCulture, $"{x:F3},{y:F3}");
```

Integers, floating-point numbers, `decimal`, `bool`, `char`, `DateTime`, `DateTimeOffset`, `TimeSpan` and `Guid` are formatted into a stack buffer, and enum values with no format or `G` are written from a name table built on first use. These holes do not allocate, with two exceptions inside Unity's `TryFormat`: `DateTime` and `DateTimeOffset` unless the format is `O` or `R` (the default format allocates too), and `TimeSpan` with `g`. Other types, other enum formats, a provider that returns an `ICustomFormatter`, and results longer than 1024 characters take the regular `ToString` path.

| You write | Unity, with this package | .NET 6 and later |
|-----------|--------------------------|------------------|
| `sb.Append($"…")` | builds the string, then calls `Append(string)` | the BCL handler |
| `sb.AppendInterpolated($"…")` | this handler | does not compile without this package |
| `sb.Append(provider, $"…")` | this handler (extension method) | the BCL handler (instance method) |

`sb.Append($"…")` cannot use the handler: an instance method always wins over an extension method of the same name. For code that also compiles for .NET 6 or later, `sb.Append(provider, $"…")` works on both sides.

## Requirements

- Unity 2022.3.12f1 or later
- Verified on Unity 2022.3.62f3, 6000.3.9f1 and 6000.6.0f1 with [tools/run-compat.sh](./tools/run-compat.sh). Results: [docs/Compatibility.md](./docs/Compatibility.md#version-matrix).

## Installation

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL...**
3. Enter:

```text
https://github.com/xpTURN/Polyfill.git?path=src/Polyfill/Assets/Polyfill
```

To stay on a release, add its tag:

```text
https://github.com/xpTURN/Polyfill.git?path=src/Polyfill/Assets/Polyfill#v0.4.0
```

### Project settings (C# language version)

Syntax newer than C# 9 needs a newer language version in two places: Unity's compiler arguments, for the build, and the generated `.csproj` files, for the IDE. One menu command sets both:

**Edit > Polyfill > Player Settings > Apply Additional Compiler Arguments -langversion (All Installed Platforms)**

The command:

- adds `-langversion:preview` to the Player settings' **Additional Compiler Arguments** of every installed platform, which Unity's compiler uses;
- adds `CSHARP_PREVIEW` to the **Scripting Define Symbols** of the same platforms, so code can check `#if CSHARP_PREVIEW`;
- regenerates the `.csproj` files with `<LangVersion>preview</LangVersion>`, so IDEs (Visual Studio, Cursor, OmniSharp and others) parse the same syntax. Later regenerations keep it.

The choice is saved in `ProjectSettings/xpTURN.Polyfill.Settings.json`. No `csc.rsp` file such as [docs/csc.rsp](./docs/csc.rsp) is needed beside your asmdefs. **Edit > Polyfill > Regenerate Project Files** rewrites the `.csproj` files at any time.

To undo, run **Edit > Polyfill > Player Settings > Remove Additional Compiler Arguments -langversion (All Installed Platforms)**.

### What `preview` accepts

`preview` means the newest language version of the compiler that the editor ships:

| Unity | Compiler | With `-langversion:preview` |
|-------|----------|-----------------------------|
| 2022.3, 6000.3 | Roslyn 4.3 | C# 10, and C# 11 except `file` types (CS0116) and `[UnscopedRef]` (ignored, so the `ref` return fails with CS8170). No C# 12. |
| 6000.6 | Roslyn 4.10 | C# 12 except inline arrays, and part of C# 13 (`params` spans, `\e`) |

Unity's release notes move the compiler forward in 6000.5. Guard code that needs Roslyn 4.10 (C# 12, `file` types, `[UnscopedRef]`) with `#if CSHARP_PREVIEW && UNITY_6000_5_OR_NEWER`.

Pitfalls:

- The argument applies to every assembly in the project, including third-party and Unity packages in `Library/PackageCache`.
- Generic attributes (`[MyAttribute<int>]`) compile on Roslyn 4.3 but break the IL2CPP build in Unity 2022.3 and 6000.3 with an `InvalidCastException` that does not name the attribute. Unity 6000.6 builds them.
- `ref` fields compile on Roslyn 4.3 without an error, but the runtime does not support them. Roslyn 4.10 reports CS9064.
- Static abstract interface members (CS8919) and `[AsyncMethodBuilder]` on a method (CS0592) compile in none of the three editors.

### Assembly Definition usage

If your project uses **Assembly Definition** (.asmdef), add a reference to this package's runtime assembly in any asmdef that uses the polyfill types (`init`, `record`, `required`, interpolated string handlers, etc.).

1. Select your **Assembly Definition** (.asmdef) file and open it in the Inspector.
2. Under **References**, click **+** and add **xpTURN.Polyfill.Runtime**.

Without this reference, scripts in that assembly will not see types such as `IsExternalInit` or `RequiredMemberAttribute` and may fail to compile.

Result after adding the package reference:

<img src="./docs/assets/Assembly-Definition-References.png" alt="Assembly Definition References" width="420">

## Usage examples

### init-only property (C# 9)

```csharp
public class Data
{
    public string Id { get; init; }
    public int Value { get; init; }
}

var d = new Data { Id = "a", Value = 1 };
```

### record (C# 9)

Works on top of the `init` polyfill. Supports value equality and `with` expressions.

```csharp
public record Point(int X, int Y);

var p = new Point(1, 2);
var q = p with { Y = 3 };  // Point(1, 3)
```

### SkipLocalsInit (C# 9)

Tells the compiler not to zero-fill the method's locals, which saves time on large `stackalloc` buffers in hot paths. Use it only where every element is written before it is read. The assembly must allow unsafe code (**Allow 'unsafe' Code** on the asmdef, otherwise CS0227); the method itself needs no `unsafe` keyword.

```csharp
using System;
using System.Runtime.CompilerServices;

[SkipLocalsInit]
static string ToHex(ReadOnlySpan<byte> data)
{
    Span<char> chars = data.Length <= 128 ? stackalloc char[data.Length * 2] : new char[data.Length * 2];
    for (int i = 0; i < data.Length; i++)
    {
        chars[i * 2] = "0123456789abcdef"[data[i] >> 4];      // every char is written before it is read
        chars[i * 2 + 1] = "0123456789abcdef"[data[i] & 0xF];
    }
    return new string(chars);
}
```

### Module initializer (C# 9)

A `[ModuleInitializer]` method runs once for its assembly without anyone calling it, which suits registration code that a source generator emits. It must be `static`, take no parameters, return `void`, and be `internal` or `public` (CS8814, CS8815 otherwise).

```csharp
using System.Runtime.CompilerServices;

static class CommandRegistration
{
    [ModuleInitializer]
    internal static void Register() => CommandRegistry.Add(typeof(MoveCommand));
}
```

When the method runs depends on where the code runs:

| Where | When module initializers run |
|-------|------------------------------|
| Editor | All of them, before user code runs (also before `[InitializeOnLoad]`) |
| IL2CPP player | All of them, at startup |
| Mono player | When the assembly is first used: a static method call, a static field read, or a call on an object. `typeof` alone does not run it, and an assembly that nothing touches never runs it. |

- In a Mono player, code that depends on another assembly's registration should run that assembly's initializer first: `RuntimeHelpers.RunModuleConstructor(typeof(SomeTypeInThatAssembly).Module.ModuleHandle)`.
- IL2CPP throws `NotSupportedException` from that call, so wrap it in `try`/`catch`. The initializers have already run there.

### CallerArgumentExpression (C# 10)

The compiler passes the **source text** of the argument for the specified parameter. Useful for assertions or diagnostics.

```csharp
using System.Runtime.CompilerServices;

static void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string expression = null)
{
    if (!condition)
        throw new System.ArgumentException($"Condition failed: {expression}");
}

Assert(x > 0);  // On failure: "Condition failed: x > 0"
```

### Custom interpolated string (C# 10)

[InterpolatedStringHandlerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerAttribute.cs) lets a method take `$"…"` as a struct you write instead of a finished `string`. The compiler turns the literal into calls on that struct, so the method decides what happens before any text exists. Our projects use it three ways:

| Use | What a `string` parameter cannot do | Used in |
|-----|-------------------------------------|---------|
| Skip disabled log levels | Build no text and run no `{…}` expression while the level is off, and drop the call from release builds | [Klotho](https://github.com/xpTURN/Klotho) logger, [XLogger](https://github.com/xpTURN/XLogger) |
| Write into a reused buffer | Update UI text without creating a string | [XString](https://github.com/xpTURN/XString) `label.SetTextX($"…")` |
| Name the values | Turn each hole into a log field named after its expression | XLogger on [ZLogger](https://github.com/Cysharp/ZLogger) |

#### Skip the work when the level is off

[InterpolatedStringHandlerArgumentAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerArgumentAttribute.cs) passes the logger to the handler's constructor, which answers whether to build the message:

```csharp
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

[InterpolatedStringHandler]
public ref struct VerboseHandler
{
    readonly StringBuilder _sb;

    public VerboseHandler(int literalLength, int formattedCount, ILogger logger, out bool enabled)
    {
        enabled = logger.IsLogTypeAllowed(LogType.Log);
        _sb = enabled ? new StringBuilder(literalLength + formattedCount * 8) : null;
    }

    public void AppendLiteral(string value) => _sb.Append(value);
    public void AppendFormatted<T>(T value) => _sb.Append(value?.ToString());
    public string Text => _sb?.ToString();
}

public static class LoggerExtensions
{
    [System.Diagnostics.Conditional("DEBUG")]   // editor and development builds
    public static void Verbose(this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref VerboseHandler message)
    {
        if (message.Text is { } text) logger.Log(LogType.Log, text);
    }
}
```

The call stays one line:

```csharp
Debug.unityLogger.Verbose($"tick={tick} state={DumpState()}");
```

Roughly what the compiler emits:

```csharp
ILogger logger = Debug.unityLogger;
var message = new VerboseHandler(12, 2, logger, out bool enabled);
if (enabled)
{
    message.AppendLiteral("tick=");
    message.AppendFormatted(tick);
    message.AppendLiteral(" state=");
    message.AppendFormatted(DumpState());
}
LoggerExtensions.Verbose(logger, ref message);
```

- With `Debug.unityLogger.filterLogType = LogType.Warning`, the call builds no text and `DumpState()` never runs. `Debug.Log($"…")` builds the whole string before Unity checks the filter.
- A release player does not define `DEBUG`, so the compiler removes the call together with the handler and its arguments.
- Klotho's logger follows this pattern with one handler per level. Its handlers write numbers with `TryFormat` into a thread-static `char[]`, so for numbers and strings an enabled call allocates only the final string.

#### Write into a reused buffer

[XString](https://github.com/xpTURN/XString) formats into ZString's pooled buffer and hands the characters to TextMesh Pro, so a label updated every frame creates no string:

```csharp
hpLabel.SetTextX($"HP {hp} / {maxHp}");
```

Inside, simplified:

```csharp
public static void SetTextX(this TMP_Text label, ref XStringHandler message)
{
    var chars = message.Builder.AsArraySegment();   // ZString Utf16ValueStringBuilder
    label.SetCharArray(chars.Array, chars.Offset, chars.Count);
    message.Builder.Dispose();                       // back to the pool
}
```

#### Name the values

`[CallerArgumentExpression]` also works on a handler's `AppendFormatted`, so each hole arrives with its source text. XLogger passes it to ZLogger, which writes it as a structured log field:

```csharp
public void AppendFormatted<T>(T value, int alignment = 0, string format = null,
    [CallerArgumentExpression("value")] string argumentName = null)
    => _inner.AppendFormatted(value, alignment, format, argumentName);
```

```csharp
logger.XLogInformation($"Player {playerId} reached {score} points");   // argumentName: "playerId", "score"
```

#### Writing your own

- The constructor takes `(int literalLength, int formattedCount)`, then the parameters named in `[InterpolatedStringHandlerArgument]`, then optionally `out bool`. Add `AppendLiteral(string)` and an `AppendFormatted` for each hole shape you accept: `{x}`, `{x:F2}`, `{x,8}`.
- A generic `AppendFormatted<T>(T value)` that calls `ToString()` allocates for every value-type hole, and `value is IFormattable` adds a box. That is two allocations per hole, more than a plain `$"…"` once there are two such holes. Add overloads for the types you log and format them with `TryFormat`, as Klotho does, or forward the holes to this package's `AppendInterpolatedStringHandler` ([StringBuilder helper](#stringbuilder-helper)) as the sample [XHandler](./samples/PolyfillSample/Assets/Scripts/InterpolatedString/XHandler.cs) does.
- A buffer kept in a static field must survive a nested call from inside a hole. Klotho takes its `[ThreadStatic]` buffer out of the field while it is in use.
- A method that takes only a handler accepts only `$"…"`. `logger.Verbose("tick=" + tick)` fails with CS1620 (*must be passed with the 'ref' keyword*), which does not say why. Add a `string` overload if callers need one.
- Next to `using UnityEngine;`, write `[System.Diagnostics.Conditional(...)]` in full. Adding `using System.Diagnostics;` makes `Debug` ambiguous (CS0104).
- Ready-made: [ZLogger](https://github.com/Cysharp/ZLogger) and [XLogger](https://github.com/xpTURN/XLogger) for logging, [ZString](https://github.com/Cysharp/ZString) and [XString](https://github.com/xpTURN/XString) for strings.

### required member (C# 11)

A `required` member must be set in the object initializer. A constructor marked `[SetsRequiredMembers]` sets it itself.

```csharp
using System.Diagnostics.CodeAnalysis;

public class Config
{
    public required string Name { get; init; }
    public int Port { get; init; } = 7777;

    public Config() { }

    [SetsRequiredMembers]
    public Config(string name) => Name = name;
}

var a = new Config { Name = "lobby" };   // OK
var b = new Config("lobby");             // OK: the constructor sets Name
var c = new Config { Port = 9000 };      // CS9035: required member 'Config.Name' must be set
```

### MonoBehaviour and namespace in Unity

Declare MonoBehaviour and ScriptableObject classes in a block namespace. Unity links a script file to its class by reading the file, and in Unity 2022.3, 6000.3 and 6000.6 that link fails for a file-scoped namespace (`namespace Game;`): `MonoScript.GetClass()` returns null, and adding the component reports *"Can't add script component 'Player' because the script class cannot be found"*.

```csharp
using UnityEngine;

namespace Game
{
    public class Player : MonoBehaviour { }
}
```

Other classes, such as handlers and loggers, may use file-scoped namespaces.

## License

The xpTURN.Polyfill code is under the Apache License, Version 2.0. See [LICENSE](./LICENSE) for details.

## Links

- **Changelog**: [CHANGELOG](./CHANGELOG.md)
- **Samples**: [samples/PolyfillSample](./samples/PolyfillSample), a Unity 2022.3 project that references this package by path
- **License**: [LICENSE](./LICENSE)
- **Author**: [xpTURN](https://github.com/xpTURN)
