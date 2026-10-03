# xpTURN.Polyfill

[English](README.md) | [한국어](README.ko-KR.md) | [日本語](README.ja-JP.md) | **简体中文**

一个包，让 Unity 项目能够编译 C# 9 到 11 的代码（`record`、`init`、`required`、自定义插值字符串处理器、模块初始化器）、Unity 6000.5 及更高版本上的 C# 12 特性，以及围绕这些语法编写的开源库。

Unity 以 .NET Standard 2.1 为目标进行编译，默认语言版本是 C# 9.0，因此缺了两样东西。这个包把两样都补上：

- **Runtime**：编译器要找的 BCL 类型（`IsExternalInit`、`RequiredMemberAttribute`、`InterpolatedStringHandlerAttribute` …），只声明一次，并以 public 提供给项目中的所有程序集。
- **Editor**：一个菜单命令，把 `-langversion:preview` 应用到 Player 构建和生成的 `.csproj` 文件，让 Unity 与 IDE 使用相同的语法。

## 为什么需要它

### Unity 留下的空缺

[Unity 手册](https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html)把语言版本定为 C# 9.0，把 *init only setters* 和 *module initializers* 列为不支持的功能，并对 record 给出如下建议：

> Users can work around this issue by declaring the `System.Runtime.CompilerServices.IsExternalInit` type in their own projects.

没有这些类型，构建会停在编译阶段：

| 你写的代码 | 缺少类型时的编译错误 |
|------------|----------------------|
| `init`、`record`、`readonly record struct` | CS0518: Predefined type `System.Runtime.CompilerServices.IsExternalInit` is not defined or imported |
| `required` | CS0656: Missing compiler required member `RequiredMemberAttribute..ctor`（以及 `CompilerFeatureRequiredAttribute..ctor`） |
| `[InterpolatedStringHandler]`、`[CallerArgumentExpression]`、`[ModuleInitializer]`、`[SkipLocalsInit]`、`[DynamicallyAccessedMembers]` | CS0246: The type or namespace name could not be found（对于 `[CallerArgumentExpression]` 和 `[DynamicallyAccessedMembers]`，Unity 6000.6 报告的是 CS0122，即因保护级别而无法访问） |

原始字符串字面量、列表模式、UTF-8 字面量和文件范围的命名空间不需要类型，只需要语言版本，而语言版本由 Editor 命令设置。不过不要把 MonoBehaviour 类放进文件范围的命名空间（[原因](#unity-中的-monobehaviour-与命名空间)）。`file` 类型同样不需要类型，但 Unity 6000.3 及更早版本的编译器会拒绝它们（CS0116），在 Unity 6000.6 中可以编译。

### 库自带的副本只供库自己使用

流行的库已经在使用这些语法。它们之所以能编译，是因为每个库都在自己的 .NET Standard 2.1 程序集中嵌入了一份相同类型的 internal 副本：

| NuGet 包 | 嵌入的支持类型（检查的 12 个中） | 可见性 |
|----------|----------------------------------|--------|
| ZLogger 2.5.10 | 12 | internal |
| Utf8StringInterpolation 1.3.1 | 11 | internal |
| R3 1.3.0 | 11 | internal |
| ZLinq 1.5.4 | 11 | internal |
| MemoryPack.Core 1.21.4 | 1（`IsExternalInit`） | internal |
| VitalRouter 2.2.0 | 0 | 不适用 |
| ZString 2.6.0 | 0 | 不适用 |

internal 副本只能让库本身通过编译，对你的程序集毫无帮助。当你的代码声明 record 命令、`required` 成员或自己的处理器时，编译器会再次查找这些类型，结果找不到。

## 案例

### 使用开源库

每一行都是库 README 中的代码，针对其 NuGet 包编译的结果。

| 库 | README 中的代码 | 没有这个包 | 有这个包 |
|----|-----------------|------------|----------|
| [ZLogger](https://github.com/Cysharp/ZLogger) 2.5.10 | `logger.ZLogInformation($"frame={frame} t={t:F2}")` | C# 9.0 下 CS8773: *interpolated string handlers* is not available | 从 C# 10 起 0 个错误 |
| [VitalRouter](https://github.com/hadashiA/VitalRouter) 2.2.0 | `public readonly record struct MoveCommand(...) : ICommand;` | CS0518 | 0 个错误 |
| [MemoryPack](https://github.com/Cysharp/MemoryPack) 1.21.4 | 使用 `required … { get; init; }` 和 `partial record` 的 `[MemoryPackable]` 类 | CS0518、CS0656 ×2 | 0 个错误 |

- **ZLogger** 只需要语言版本。它的 Unity 指南要求在每个引用 ZLogger 的 asmdef 旁放一个 `csc.rsp`，并为 IDE 准备 `LangVersion.props` 和 CsprojModifier。这个包的 Editor 命令可以替代这三样。
- **VitalRouter** 推荐使用 `readonly record struct` 命令，却不自带 `IsExternalInit`，所以在类型出现之前，推荐的写法无法编译。
- **MemoryPack** 的模型使用 `required` 和 `init`，它的生成器还会向你的程序集输出 `scoped ref`（C# 11）。这个包的两部分都需要。

### 在库之上构建

- [xpTURN.XLogger](https://github.com/xpTURN/XLogger) 用可以从发布构建中剥离的 `XLog*` 方法包装 ZLogger。它用 `[InterpolatedStringHandler]` 声明了 7 个处理器，并借助 `[CallerArgumentExpression]` 和 `[DynamicallyAccessedMembers]` 沿用 ZLogger 的 `AppendFormatted` 签名。这三个类型都来自这个包。
- [xpTURN.XString](https://github.com/xpTURN/XString) 用自己的处理器，在 ZString 的 `Utf16ValueStringBuilder` 之上提供 `XString.Format($"...")`。

### 替换依赖

[xpTURN.Klotho](https://github.com/xpTURN/Klotho) 移除了 ZLogger 及其整个依赖图，共 13 个 NuGet DLL。它自己的日志器通过六个 `ref struct` 处理器保留了 `$"..."` 的调用方式：65 个文件中有超过 500 处插值日志调用。日志程序集只引用 `xpTURN.Polyfill.Runtime`。

### 源生成器与模块初始化器

Klotho 的源生成器会为组件、数据资源，以及在其他程序集中声明的命令和消息生成 `[ModuleInitializer]` 注册代码，让游戏类型无需手写列表就能自行注册。Unity 把模块初始化器列为不支持，MemoryPack 的 README 也让 Unity 用户手动注册 union 格式化器。Klotho 使用这个包的 `ModuleInitializerAttribute`，并在首次读取注册表之前用 `RuntimeHelpers.RunModuleConstructor` 自行运行模块构造函数。

### 我们自己项目的前后对比

| | 之前（内部客户端，2026 年 1 月） | 之后（使用相同 VContainer + VitalRouter + ZLogger 组合的示例） |
|-|----------------------------------|----------------------------------------------------------------|
| 构建的语言版本 | 每个 asmdef 文件夹一个，共 6 个 `csc.rsp` 文件 | 0 个 `csc.rsp` 文件 |
| IDE 的语言版本 | `LangVersion.props` + `Directory.Build.props` + CsprojModifier | 自动 |
| 支持类型 | 项目专用 asmdef 中手工复制的 4 个文件 | 包引用 |
| 设置 | 每个新 asmdef 都要重复 | 1 个包，1 次菜单命令 |

我们有九个 Unity 项目在 Unity 2022.3.62f3、6000.3.9f1 和 6000.6.0f1 上引用这个包。

### 什么时候不需要它

- 你的代码停留在 C# 9 以内，不使用 `init` 或 `record`，项目中也没有库要求调用方使用更新的语法。
- 只有一个 asmdef 需要更新的语言版本，而且不需要这些类型：在那个 asmdef 旁放一个 `csc.rsp` 就够了。
- 你需要依赖运行时的功能，例如 static abstract 接口成员。任何 polyfill 都无法添加这些功能。

> 测量方法：[docs/Compatibility.md](./docs/Compatibility.md#case-studies)

## 与其他方案的比较

为 .NET 项目编写的 polyfill 库都假定有较新的编译器和 MSBuild 属性。Unity 两者都没有：它固定使用自己的 Roslyn（2022.3 和 6000.3 是 4.3，6000.6 是 4.10），并且不经过 MSBuild 构建。所以同一个库在 Unity 中的表现可能不同。

最后一列表示在启用 `-langversion:preview` 的 Unity 2022.3、6000.3 和 6000.6 中，12 个依赖类型的功能里哪些能够编译：`init`、`record`、`readonly record struct`、`required`、`[SetsRequiredMembers]`、`[InterpolatedStringHandler]`、`[InterpolatedStringHandlerArgument]`、`[CallerArgumentExpression]`、`[SkipLocalsInit]`、`[ModuleInitializer]`、`[DynamicallyAccessedMembers]` 和 `[UnscopedRef]`（Roslyn 4.3 会忽略它，所以只在 6000.6 上）。各编辑器的数量、测量方法以及生成器失败的原因：[docs/Compatibility.md](./docs/Compatibility.md#other-polyfill-libraries)

| 方案 | 形式 | 设置语言版本 | 能编译的功能 |
|------|------|--------------|--------------|
| 不用 polyfill | 不适用 | 不适用 | 无 |
| **xpTURN.Polyfill 0.4.0** | UPM，一个共享程序集，public 类型 | 是：构建、包、IDE | 全部 |
| [PolySharp](https://github.com/Sergio0694/PolySharp) 1.15.0、1.16.0 | NuGet 源生成器，每个程序集一份 internal 类型 | 否 | 除 `[DynamicallyAccessedMembers]` 外全部 |
| 启用 `PolySharpIncludeRuntimeSupportedAttributes` 的 PolySharp 1.16.0 | 同上 | 否 | 无（CS8336）；用 `.globalconfig` 同时关闭嵌入特性后为全部 |
| [PolyfillLib](https://github.com/SimonCropp/Polyfill) 11.4.1 | 附带 2 个依赖 DLL 的 NuGet DLL，public 类型 | 否 | 全部 |
| [Polyfill](https://github.com/SimonCropp/Polyfill) 11.4.1（仅源码） | NuGet 源文件 | 否 | 无：C# 14 语法 |
| [Meziantou.Polyfill](https://github.com/meziantou/Meziantou.Polyfill) 1.0.165 | NuGet 源生成器 | 否 | 无：重复定义，6000.6 上为 CS8336 |
| [ModernCSharpForUnity](https://github.com/iAcolyte/ModernCSharpForUnity) 0.1.0 | UPM 源生成器 + Project Settings 页面 | 是：`Assets` 中每个 asmdef 一个 `csc.rsp`，C# 9 到 12 | 仅在 6000.6 上，12 个中 9 个。Roslyn 4.3 不加载该生成器（CS9057），且该包要求 Unity 6000.0 |
| [IsExternalInit](https://github.com/CorundumGames/IsExternalInit) 1.0.0 | OpenUPM DLL，public 类型 | 否 | `init`、`record` 和 `readonly record struct` |

这个包是不含生成器的普通 C# 源码，因此不依赖 Roslyn 版本。这就是一次设置能覆盖 2022.3 到 6000.6 的原因。

其他方案更强的地方：

- 生成器会给每个程序集一份自己的 `internal` 副本，不需要 asmdef 引用，两份副本也不会冲突。
- PolySharp 和 PolyfillLib 覆盖更多类型，例如裁剪注解（`RequiresUnreferencedCode`、`DynamicDependency`），PolyfillLib 还补充了 .NET Standard 2.1 缺少的 BCL 方法。Unity 的编译器和 IDE 不会用这些裁剪注解做任何事，所以这个包没有收录它们。
- ModernCSharpForUnity 可以按项目选择语言版本和 nullable 设置。这个包只应用 `preview`。

选择之前需要了解的限制：

- 类型以 `public` 形式放在一个共享程序集中，因此每个使用它们的 asmdef 都需要引用 `xpTURN.Polyfill.Runtime`。
- 不要引用第二个以 public 形式公开相同类型的程序集。同时使用这个包和一个声明了 public `IsExternalInit` 的 DLL 时，`init` 会以 CS0518 失败。此时 Editor 会在警告中指出那个程序集的名称。
- 如果用上面的 `.globalconfig` 同时设置两个选项，它可以与 PolySharp 1.16.0 并存编译。
- 运行时缺少的东西，任何 polyfill 都补不上：static abstract 接口成员（泛型数学）、`ref` 字段和内联数组。

## 提供的类型

| C# 版本 | 功能 | 类型 |
|---------|------|------|
| C# 9  | 仅 `init` 的 setter | [IsExternalInit](./src/Polyfill/Assets/Polyfill/Runtime/Init/IsExternalInit.cs) |
| C# 9  | 模块初始化器 | [ModuleInitializerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Module/ModuleInitializerAttribute.cs) |
| C# 9  | 跳过局部变量初始化 | [SkipLocalsInitAttribute](./src/Polyfill/Assets/Polyfill/Runtime/SkipLocalsInit/SkipLocalsInitAttribute.cs) |
| C# 9  | 动态访问的成员（裁剪器分析） | [DynamicallyAccessedMembersAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/DynamicallyAccessedMembersAttribute.cs)、[DynamicallyAccessedMemberTypes](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/DynamicallyAccessedMemberTypes.cs) |
| C# 9  | 由方法设置的成员的 nullable 分析 | [MemberNotNullAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/MemberNotNullAttribute.cs)、[MemberNotNullWhenAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/MemberNotNullWhenAttribute.cs) |
| C# 10 | 自定义插值字符串处理器 | [InterpolatedStringHandlerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerAttribute.cs)、[InterpolatedStringHandlerArgumentAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerArgumentAttribute.cs) |
| C# 10 | 调用方参数表达式 | [CallerArgumentExpressionAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Caller/CallerArgumentExpressionAttribute.cs) |
| C# 11 | `required` 成员 | [RequiredMemberAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/RequiredMemberAttribute.cs) |
| C# 11 | 满足 `required` 的构造函数 | [SetsRequiredMembersAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/SetsRequiredMembersAttribute.cs) |
| C# 11 | 编译器功能要求 | [CompilerFeatureRequiredAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/CompilerFeatureRequiredAttribute.cs) |
| C# 11 | 指向结构体自身字段的 `ref`（Unity 6000.5 及更高版本；更早的编译器会忽略） | [UnscopedRefAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/UnscopedRefAttribute.cs) |
| C# 12 | 自定义类型的集合表达式（Unity 6000.5 及更高版本） | [CollectionBuilderAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Collections/CollectionBuilderAttribute.cs) |
| C# 12 | 实验性 API 的诊断（Unity 6000.5 及更高版本） | [ExperimentalAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/ExperimentalAttribute.cs) |
| IDE   | 字符串参数的语言（正则表达式、JSON、日期格式） | [StringSyntaxAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/StringSyntaxAttribute.cs) |

每个类型只在目标框架缺少它时才会编译（`#if !NET5_0_OR_GREATER` 等），因此运行时自带同名类型时会让位给它。

### StringBuilder 辅助方法

`sb.AppendInterpolated($"…")` 通过 `AppendInterpolatedStringHandler` 追加内容，而不是先构建字符串：

```csharp
using System.Globalization;
using System.Text;

var sb = new StringBuilder();
sb.AppendInterpolated($"frame={frame} t={t:F2} state={state}");
sb.AppendInterpolated(CultureInfo.InvariantCulture, $"{x:F3},{y:F3}");
```

整数、浮点数、`decimal`、`bool`、`char`、`DateTime`、`DateTimeOffset`、`TimeSpan` 和 `Guid` 会格式化到栈缓冲区中；没有格式或格式为 `G` 的枚举值则从首次使用时建立的名称表写入。这些插值孔不会分配内存，但在 Unity 的 `TryFormat` 内部有两个例外：格式不是 `O` 或 `R` 的 `DateTime` 和 `DateTimeOffset`（默认格式也会分配），以及使用 `g` 格式的 `TimeSpan`。其他类型、枚举的其他格式、返回 `ICustomFormatter` 的 provider，以及超过 1024 个字符的结果，走原来的 `ToString` 路径。

| 你写的代码 | Unity（使用这个包） | .NET 6 及更高版本 |
|------------|---------------------|-------------------|
| `sb.Append($"…")` | 先构建字符串，再调用 `Append(string)` | BCL 处理器 |
| `sb.AppendInterpolated($"…")` | 这个处理器 | 没有这个包就无法编译 |
| `sb.Append(provider, $"…")` | 这个处理器（扩展方法） | BCL 处理器（实例方法） |

`sb.Append($"…")` 无法使用这个处理器：同名时实例方法总是优先于扩展方法。对于也要在 .NET 6 及更高版本上编译的代码，`sb.Append(provider, $"…")` 两边都能工作。

## 要求

- Unity 2022.3.12f1 或更高版本
- 已在 Unity 2022.3.62f3、6000.3.9f1 和 6000.6.0f1 上用 [tools/run-compat.sh](./tools/run-compat.sh) 验证。结果：[docs/Compatibility.md](./docs/Compatibility.md#version-matrix)

## 安装

1. 打开 **Window > Package Manager**
2. 点击 **+** > **Add package from git URL...**
3. 输入：

```text
https://github.com/xpTURN/Polyfill.git?path=src/Polyfill/Assets/Polyfill
```

如果要固定在某个发布版本，加上它的标签：

```text
https://github.com/xpTURN/Polyfill.git?path=src/Polyfill/Assets/Polyfill#v0.4.0
```

### 项目设置 (C# 语言版本)

比 C# 9 更新的语法需要在两个地方设置更新的语言版本：用于构建的 Unity 编译器参数，以及用于 IDE 的生成的 `.csproj` 文件。一个菜单命令同时设置两者：

**Edit > Polyfill > Player Settings > Apply Additional Compiler Arguments -langversion (All Installed Platforms)**

这个命令会：

- 为每个已安装的平台，在 Player 设置的 **Additional Compiler Arguments** 中加入 `-langversion:preview`，供 Unity 的编译器使用；
- 在相同平台的 **Scripting Define Symbols** 中加入 `CSHARP_PREVIEW`，让代码可以用 `#if CSHARP_PREVIEW` 判断；
- 以 `<LangVersion>preview</LangVersion>` 重新生成 `.csproj` 文件，让 IDE（Visual Studio、Cursor、OmniSharp 等）解析相同的语法。之后重新生成时也会保留。

这一选择保存在 `ProjectSettings/xpTURN.Polyfill.Settings.json` 中。不需要在 asmdef 旁放置 [docs/csc.rsp](./docs/csc.rsp) 这样的 `csc.rsp` 文件。**Edit > Polyfill > Regenerate Project Files** 可以随时重写 `.csproj` 文件。

要撤销，运行 **Edit > Polyfill > Player Settings > Remove Additional Compiler Arguments -langversion (All Installed Platforms)**。

### `preview` 接受哪些语法

`preview` 指编辑器自带编译器的最新语言版本：

| Unity | 编译器 | 使用 `-langversion:preview` 时 |
|-------|--------|--------------------------------|
| 2022.3、6000.3 | Roslyn 4.3 | C# 10，以及除 `file` 类型（CS0116）和 `[UnscopedRef]`（会被忽略，因此 `ref` 返回会以 CS8170 失败）之外的 C# 11。不支持 C# 12。 |
| 6000.6 | Roslyn 4.10 | 除内联数组外的 C# 12，以及部分 C# 13（`params` span、`\e`） |

根据 Unity 的发布说明，6000.5 升级了编译器。需要 Roslyn 4.10 的代码（C# 12、`file` 类型、`[UnscopedRef]`）请用 `#if CSHARP_PREVIEW && UNITY_6000_5_OR_NEWER` 包起来。

陷阱：

- 这个参数会作用于项目中的所有程序集，包括 `Library/PackageCache` 中的第三方包和 Unity 包。
- 泛型特性（`[MyAttribute<int>]`）在 Roslyn 4.3 上能编译，但会让 Unity 2022.3 和 6000.3 的 IL2CPP 构建以 `InvalidCastException` 失败，而且错误不会指出是哪个特性。Unity 6000.6 可以构建。
- `ref` 字段在 Roslyn 4.3 上可以无错误地编译，但运行时并不支持。Roslyn 4.10 会报告 CS9064。
- static abstract 接口成员（CS8919）和加在方法上的 `[AsyncMethodBuilder]`（CS0592）在三个编辑器中都无法编译。

### 使用 Assembly Definition

如果你的项目使用 **Assembly Definition**（.asmdef），请在每个使用 polyfill 类型（`init`、`record`、`required`、插值字符串处理器等）的 asmdef 中添加对这个包的运行时程序集的引用。

1. 选中你的 **Assembly Definition**（.asmdef）文件，在 Inspector 中打开。
2. 在 **References** 下点击 **+**，添加 **xpTURN.Polyfill.Runtime**。

没有这个引用，该程序集中的脚本就看不到 `IsExternalInit` 或 `RequiredMemberAttribute` 等类型，可能无法编译。

添加包引用后的结果：

<img src="./docs/assets/Assembly-Definition-References.png" alt="Assembly Definition References" width="420">

## 使用示例

### 仅 init 的属性 (C# 9)

```csharp
public class Data
{
    public string Id { get; init; }
    public int Value { get; init; }
}

var d = new Data { Id = "a", Value = 1 };
```

### record (C# 9)

基于 `init` 的 polyfill 工作，支持值相等和 `with` 表达式。

```csharp
public record Point(int X, int Y);

var p = new Point(1, 2);
var q = p with { Y = 3 };  // Point(1, 3)
```

### SkipLocalsInit (C# 9)

告诉编译器不要把方法的局部变量清零，可以在热点路径的大块 `stackalloc` 缓冲区上节省时间。只在每个元素都先写后读的地方使用。程序集必须允许不安全代码（asmdef 的 **Allow 'unsafe' Code**，否则报 CS0227），但方法本身不需要 `unsafe` 关键字。

```csharp
using System;
using System.Runtime.CompilerServices;

[SkipLocalsInit]
static string ToHex(ReadOnlySpan<byte> data)
{
    Span<char> chars = data.Length <= 128 ? stackalloc char[data.Length * 2] : new char[data.Length * 2];
    for (int i = 0; i < data.Length; i++)
    {
        chars[i * 2] = "0123456789abcdef"[data[i] >> 4];      // 每个字符都先写后读
        chars[i * 2 + 1] = "0123456789abcdef"[data[i] & 0xF];
    }
    return new string(chars);
}
```

### 模块初始化器 (C# 9)

`[ModuleInitializer]` 方法无需任何调用，就会在其程序集中运行一次，适合放源生成器输出的注册代码。它必须是 `static`、没有参数、返回 `void`，并且是 `internal` 或 `public`（否则报 CS8814、CS8815）。

```csharp
using System.Runtime.CompilerServices;

static class CommandRegistration
{
    [ModuleInitializer]
    internal static void Register() => CommandRegistry.Add(typeof(MoveCommand));
}
```

方法何时运行，取决于代码在哪里运行：

| 在哪里 | 模块初始化器何时运行 |
|--------|----------------------|
| 编辑器 | 在用户代码运行之前全部运行（也早于 `[InitializeOnLoad]`） |
| IL2CPP 播放器 | 启动时全部运行 |
| Mono 播放器 | 程序集首次被使用时：调用静态方法、读取静态字段或调用对象的方法。仅用 `typeof` 不会触发；没有任何代码接触的程序集永远不会运行。 |

- 在 Mono 播放器中，依赖其他程序集注册结果的代码应先运行那个程序集的初始化器：`RuntimeHelpers.RunModuleConstructor(typeof(SomeTypeInThatAssembly).Module.ModuleHandle)`。
- IL2CPP 会在这个调用中抛出 `NotSupportedException`，所以要用 `try`/`catch` 包起来。在 IL2CPP 中，初始化器已经运行过了。

### CallerArgumentExpression (C# 10)

编译器会把实参的**源代码文本**传给指定的形参。适合用于断言或诊断。

```csharp
using System.Runtime.CompilerServices;

static void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string expression = null)
{
    if (!condition)
        throw new System.ArgumentException($"Condition failed: {expression}");
}

Assert(x > 0);  // 失败时: "Condition failed: x > 0"
```

### 自定义插值字符串 (C# 10)

借助 [InterpolatedStringHandlerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerAttribute.cs)，方法可以用你自己编写的结构体来接收 `$"…"`，而不是一个已经完成的 `string`。编译器会把字面量转换为对该结构体的一系列调用，因此方法可以在文本产生之前决定怎么做。我们的项目有三种用法：

| 用法 | `string` 参数做不到的事 | 使用位置 |
|------|-------------------------|----------|
| 跳过已关闭的日志级别 | 级别关闭时不构建文本、不执行任何 `{…}` 表达式，并在发布构建中删除该调用 | [Klotho](https://github.com/xpTURN/Klotho) 的日志器、[XLogger](https://github.com/xpTURN/XLogger) |
| 写入可复用的缓冲区 | 更新 UI 文本而不创建字符串 | [XString](https://github.com/xpTURN/XString) `label.SetTextX($"…")` |
| 给值命名 | 把每个插值孔变成以其表达式命名的日志字段 | 基于 [ZLogger](https://github.com/Cysharp/ZLogger) 的 XLogger |

#### 级别关闭时跳过工作

[InterpolatedStringHandlerArgumentAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerArgumentAttribute.cs) 把日志器传给处理器的构造函数，由构造函数回答是否要构建这条消息：

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
    [System.Diagnostics.Conditional("DEBUG")]   // 编辑器与开发构建
    public static void Verbose(this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref VerboseHandler message)
    {
        if (message.Text is { } text) logger.Log(LogType.Log, text);
    }
}
```

调用仍然只有一行：

```csharp
Debug.unityLogger.Verbose($"tick={tick} state={DumpState()}");
```

编译器生成的代码大致如下：

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

- 当 `Debug.unityLogger.filterLogType = LogType.Warning` 时，这次调用不会构建文本，`DumpState()` 也不会运行。而 `Debug.Log($"…")` 会在 Unity 检查过滤器之前就构建出整个字符串。
- 发布版播放器不定义 `DEBUG`，所以编译器会把这次调用连同处理器和参数一起删除。
- Klotho 的日志器就是这种模式，每个级别一个处理器。它的处理器用 `TryFormat` 把数字写入线程静态的 `char[]`，因此对数字和字符串来说，启用的调用也只分配最终的那一个字符串。

#### 写入可复用的缓冲区

[XString](https://github.com/xpTURN/XString) 把内容格式化到 ZString 的池化缓冲区，再把这些字符交给 TextMesh Pro，因此即使每帧都更新的标签也不会创建字符串：

```csharp
hpLabel.SetTextX($"HP {hp} / {maxHp}");
```

内部实现（简化后）：

```csharp
public static void SetTextX(this TMP_Text label, ref XStringHandler message)
{
    var chars = message.Builder.AsArraySegment();   // ZString Utf16ValueStringBuilder
    label.SetCharArray(chars.Array, chars.Offset, chars.Count);
    message.Builder.Dispose();                       // 归还到池中
}
```

#### 给值命名

`[CallerArgumentExpression]` 同样适用于处理器的 `AppendFormatted`，因此每个插值孔都会带上它的源代码文本。XLogger 把它传给 ZLogger，ZLogger 将其写成结构化日志字段：

```csharp
public void AppendFormatted<T>(T value, int alignment = 0, string format = null,
    [CallerArgumentExpression("value")] string argumentName = null)
    => _inner.AppendFormatted(value, alignment, format, argumentName);
```

```csharp
logger.XLogInformation($"Player {playerId} reached {score} points");   // argumentName: "playerId", "score"
```

#### 自己编写时

- 构造函数先接收 `(int literalLength, int formattedCount)`，然后是 `[InterpolatedStringHandlerArgument]` 中列出的参数，最后可选 `out bool`。再加上 `AppendLiteral(string)`，以及为你接受的每种插值孔形式（`{x}`、`{x:F2}`、`{x,8}`）各写一个 `AppendFormatted`。
- 调用 `ToString()` 的泛型 `AppendFormatted<T>(T value)` 会为每个值类型插值孔分配内存，而 `value is IFormattable` 还会再加一次装箱。每个插值孔分配两次，只要有两个这样的插值孔，就比普通的 `$"…"` 分配得更多。请为要记录的类型添加重载并像 Klotho 那样用 `TryFormat` 格式化，或者像示例 [XHandler](./samples/PolyfillSample/Assets/Scripts/InterpolatedString/XHandler.cs) 那样，把插值孔转交给这个包的 `AppendInterpolatedStringHandler`（[StringBuilder 辅助方法](#stringbuilder-辅助方法)）。
- 放在静态字段中的缓冲区必须经得起插值孔内部发生的嵌套调用。Klotho 在使用期间会把 `[ThreadStatic]` 缓冲区从字段中取出。
- 只接收处理器的方法只接受 `$"…"`。`logger.Verbose("tick=" + tick)` 会以 CS1620（*must be passed with the 'ref' keyword*）失败，而这条消息不会说明原因。如果调用方需要，请添加 `string` 重载。
- 与 `using UnityEngine;` 一起使用时，请完整写出 `[System.Diagnostics.Conditional(...)]`。加上 `using System.Diagnostics;` 会让 `Debug` 产生歧义（CS0104）。
- 现成的选择：日志用 [ZLogger](https://github.com/Cysharp/ZLogger) 和 [XLogger](https://github.com/xpTURN/XLogger)，字符串用 [ZString](https://github.com/Cysharp/ZString) 和 [XString](https://github.com/xpTURN/XString)。

### required 成员 (C# 11)

`required` 成员必须在对象初始化器中设置。标有 `[SetsRequiredMembers]` 的构造函数会自己设置它。

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
var b = new Config("lobby");             // OK: 构造函数设置了 Name
var c = new Config { Port = 9000 };      // CS9035: required member 'Config.Name' must be set
```

### Unity 中的 MonoBehaviour 与命名空间

请在块形式的命名空间中声明 MonoBehaviour 和 ScriptableObject 类。Unity 通过读取文件把脚本文件和类关联起来，而在 Unity 2022.3、6000.3 和 6000.6 中，文件范围的命名空间（`namespace Game;`）会让这种关联失败：`MonoScript.GetClass()` 返回 null，添加组件时会出现 *"Can't add script component 'Player' because the script class cannot be found"*。

```csharp
using UnityEngine;

namespace Game
{
    public class Player : MonoBehaviour { }
}
```

处理器、日志器等其他类可以使用文件范围的命名空间。

## 许可证

xpTURN.Polyfill 的代码采用 Apache License, Version 2.0 许可。详见 [LICENSE](./LICENSE)。

## 链接

- **更新日志**：[CHANGELOG](./CHANGELOG.md)
- **示例**：[samples/PolyfillSample](./samples/PolyfillSample)，通过路径引用本包的 Unity 2022.3 项目
- **许可证**：[LICENSE](./LICENSE)
- **作者**：[xpTURN](https://github.com/xpTURN)
