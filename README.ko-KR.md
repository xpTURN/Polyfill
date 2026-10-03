# xpTURN.Polyfill

[English](README.md) | **한국어** | [日本語](README.ja-JP.md) | [简体中文](README.zh-CN.md)

Unity 프로젝트에서 C# 9 ~ 11 코드(`record`, `init`, `required`, 커스텀 보간 문자열 핸들러, 모듈 이니셜라이저), Unity 6000.5 이상의 C# 12 특성, 그리고 이 문법으로 작성된 오픈소스 라이브러리를 컴파일할 수 있게 하는 패키지입니다.

Unity 는 .NET Standard 2.1 을 대상으로 컴파일하고 기본 언어 버전이 C# 9.0 이라서 두 가지가 빠져 있습니다. 이 패키지가 둘 다 채웁니다.

- **Runtime**: 컴파일러가 찾는 BCL 타입(`IsExternalInit`, `RequiredMemberAttribute`, `InterpolatedStringHandlerAttribute` …)을 한 번, 프로젝트의 모든 어셈블리가 쓸 수 있도록 public 으로 선언합니다.
- **Editor**: 메뉴 명령 하나가 Player 빌드와 생성되는 `.csproj` 파일에 `-langversion:preview` 를 적용해 Unity 와 IDE 가 같은 문법을 쓰게 합니다.

## 왜 필요한가

### Unity 가 남겨 둔 빈자리

[Unity 매뉴얼](https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html)은 언어 버전을 C# 9.0 으로 정하고, *init only setters* 와 *module initializers* 를 지원하지 않는 기능으로 적으며, record 에 대해 이렇게 안내합니다.

> Users can work around this issue by declaring the `System.Runtime.CompilerServices.IsExternalInit` type in their own projects.

타입이 없으면 빌드는 컴파일 단계에서 멈춥니다.

| 작성한 코드 | 타입이 없을 때의 컴파일 오류 |
|-------------|------------------------------|
| `init`, `record`, `readonly record struct` | CS0518: Predefined type `System.Runtime.CompilerServices.IsExternalInit` is not defined or imported |
| `required` | CS0656: Missing compiler required member `RequiredMemberAttribute..ctor` (그리고 `CompilerFeatureRequiredAttribute..ctor`) |
| `[InterpolatedStringHandler]`, `[CallerArgumentExpression]`, `[ModuleInitializer]`, `[SkipLocalsInit]`, `[DynamicallyAccessedMembers]` | CS0246: The type or namespace name could not be found (Unity 6000.6 은 `[CallerArgumentExpression]` 과 `[DynamicallyAccessedMembers]` 에 대해, 보호 수준 때문에 접근할 수 없다는 CS0122 를 냅니다) |

Raw 문자열 리터럴, 목록 패턴, UTF-8 리터럴, file-scoped 네임스페이스는 타입 없이 언어 버전만 있으면 되고, 그 언어 버전은 Editor 명령이 설정합니다. 다만 MonoBehaviour 클래스는 file-scoped 네임스페이스에 두지 마세요([이유](#unity-의-monobehaviour-와-네임스페이스)). `file` 타입도 타입이 필요 없지만 Unity 6000.3 이하의 컴파일러는 이를 거부합니다(CS0116). Unity 6000.6 에서는 컴파일됩니다.

### 라이브러리의 사본은 라이브러리 자신만 씁니다

인기 라이브러리는 이미 이 문법을 씁니다. 각 라이브러리가 .NET Standard 2.1 어셈블리 안에 같은 타입의 internal 사본을 넣어 두었기 때문에 컴파일됩니다.

| NuGet 패키지 | 내장한 지원 타입 (확인한 12개 중) | 가시성 |
|--------------|-----------------------------------|--------|
| ZLogger 2.5.10 | 12 | internal |
| Utf8StringInterpolation 1.3.1 | 11 | internal |
| R3 1.3.0 | 11 | internal |
| ZLinq 1.5.4 | 11 | internal |
| MemoryPack.Core 1.21.4 | 1 (`IsExternalInit`) | internal |
| VitalRouter 2.2.0 | 0 | 해당 없음 |
| ZString 2.6.0 | 0 | 해당 없음 |

internal 사본은 라이브러리를 컴파일되게 할 뿐, 여러분의 어셈블리에는 아무 도움이 되지 않습니다. 여러분의 코드가 record 명령, `required` 멤버, 자체 핸들러를 선언하면 컴파일러는 타입을 다시 찾고, 찾지 못합니다.

## 사례

### 오픈소스 라이브러리 쓰기

각 행은 라이브러리 README 에 있는 코드를 그 NuGet 패키지로 컴파일한 결과입니다.

| 라이브러리 | README 의 코드 | 이 패키지 없이 | 이 패키지와 함께 |
|------------|----------------|----------------|------------------|
| [ZLogger](https://github.com/Cysharp/ZLogger) 2.5.10 | `logger.ZLogInformation($"frame={frame} t={t:F2}")` | C# 9.0 에서 CS8773: *interpolated string handlers* is not available | C# 10 부터 오류 0 |
| [VitalRouter](https://github.com/hadashiA/VitalRouter) 2.2.0 | `public readonly record struct MoveCommand(...) : ICommand;` | CS0518 | 오류 0 |
| [MemoryPack](https://github.com/Cysharp/MemoryPack) 1.21.4 | `required … { get; init; }` 와 `partial record` 를 쓴 `[MemoryPackable]` 클래스 | CS0518, CS0656 ×2 | 오류 0 |

- **ZLogger** 는 언어 버전만 있으면 됩니다. ZLogger 의 Unity 안내는 ZLogger 를 참조하는 모든 asmdef 옆에 `csc.rsp` 를 두고, IDE 를 위해 `LangVersion.props` 와 CsprojModifier 까지 쓰라고 합니다. 이 패키지의 Editor 명령이 셋을 모두 대신합니다.
- **VitalRouter** 는 `readonly record struct` 명령을 권하지만 `IsExternalInit` 를 제공하지 않아, 타입이 생기기 전까지 권장 형태가 컴파일되지 않습니다.
- **MemoryPack** 모델은 `required` 와 `init` 을 쓰고, 생성기가 여러분의 어셈블리에 `scoped ref`(C# 11)를 만들어 넣습니다. 이 패키지의 두 부분이 모두 필요합니다.

### 라이브러리 위에 쌓기

- [xpTURN.XLogger](https://github.com/xpTURN/XLogger) 는 릴리스 빌드에서 지울 수 있는 `XLog*` 메서드로 ZLogger 를 감쌉니다. `[InterpolatedStringHandler]` 핸들러 7개를 선언하고, `[CallerArgumentExpression]` 과 `[DynamicallyAccessedMembers]` 로 ZLogger 의 `AppendFormatted` 시그니처를 그대로 따릅니다. 세 타입 모두 이 패키지에서 옵니다.
- [xpTURN.XString](https://github.com/xpTURN/XString) 은 자체 핸들러로 ZString 의 `Utf16ValueStringBuilder` 위에 `XString.Format($"...")` 를 더합니다.

### 의존성 걷어내기

[xpTURN.Klotho](https://github.com/xpTURN/Klotho) 는 ZLogger 를 그 의존성 그래프(NuGet DLL 모두 13개)와 함께 걷어냈습니다. 자체 로거는 `ref struct` 핸들러 여섯 개로 `$"..."` 호출 방식을 그대로 유지합니다. 파일 65개에 걸친 보간 로그 호출이 500개가 넘습니다. 로깅 어셈블리는 `xpTURN.Polyfill.Runtime` 만 참조합니다.

### 소스 생성기와 모듈 이니셜라이저

Klotho 소스 생성기는 컴포넌트, 데이터 에셋, 그리고 다른 어셈블리에 선언된 명령과 메시지를 위한 `[ModuleInitializer]` 등록 코드를 만들어, 손으로 쓴 목록 없이 게임 타입이 스스로 등록되게 합니다. Unity 는 모듈 이니셜라이저를 지원하지 않는 기능으로 적고, MemoryPack 의 README 는 Unity 사용자에게 union 포매터를 손으로 등록하라고 합니다. Klotho 는 이 패키지의 `ModuleInitializerAttribute` 를 쓰고, 등록부를 처음 읽기 전에 `RuntimeHelpers.RunModuleConstructor` 로 모듈 생성자를 직접 실행합니다.

### 우리 프로젝트의 전과 후

| | 전 (사내 클라이언트, 2026년 1월) | 후 (같은 VContainer + VitalRouter + ZLogger 구성의 샘플) |
|-|----------------------------------|----------------------------------------------------------|
| 빌드의 언어 버전 | asmdef 폴더마다 하나씩, `csc.rsp` 파일 6개 | `csc.rsp` 파일 0개 |
| IDE 의 언어 버전 | `LangVersion.props` + `Directory.Build.props` + CsprojModifier | 자동 |
| 지원 타입 | 프로젝트 전용 asmdef 에 손으로 복사한 파일 4개 | 패키지 참조 |
| 설정 | 새 asmdef 마다 반복 | 패키지 1개, 메뉴 명령 1번 |

우리 Unity 프로젝트 아홉 개가 Unity 2022.3.62f3, 6000.3.9f1, 6000.6.0f1 에서 이 패키지를 참조합니다.

### 필요 없는 경우

- 코드가 `init` 이나 `record` 없이 C# 9 안에 머물고, 프로젝트의 어떤 라이브러리도 호출하는 쪽에 더 새로운 문법을 요구하지 않을 때.
- asmdef 하나만 더 새로운 언어 버전이 필요하고 타입은 필요 없을 때: 그 asmdef 옆의 `csc.rsp` 로 충분합니다.
- static abstract 인터페이스 멤버처럼 런타임에 기대는 기능이 필요할 때. 어떤 폴리필도 이를 더할 수 없습니다.

> 측정 방법: [docs/Compatibility.md](./docs/Compatibility.md#case-studies).

## 다른 선택지와 비교

.NET 프로젝트용으로 만든 폴리필 라이브러리는 최신 컴파일러와 MSBuild 속성을 전제로 합니다. Unity 에는 둘 다 없습니다. Unity 는 자체 Roslyn(2022.3 과 6000.3 은 4.3, 6000.6 은 4.10)을 고정해 쓰고 MSBuild 없이 빌드합니다. 그래서 같은 라이브러리도 Unity 안에서는 다르게 동작할 수 있습니다.

마지막 열은 `-langversion:preview` 를 켠 Unity 2022.3, 6000.3, 6000.6 에서 타입에 기대는 기능 12개 중 무엇이 컴파일되는지 보여 줍니다: `init`, `record`, `readonly record struct`, `required`, `[SetsRequiredMembers]`, `[InterpolatedStringHandler]`, `[InterpolatedStringHandlerArgument]`, `[CallerArgumentExpression]`, `[SkipLocalsInit]`, `[ModuleInitializer]`, `[DynamicallyAccessedMembers]`, `[UnscopedRef]`(Roslyn 4.3 이 무시하므로 6000.6 에서만). 에디터별 개수, 측정 방법, 생성기가 실패하는 이유: [docs/Compatibility.md](./docs/Compatibility.md#other-polyfill-libraries).

| 선택지 | 형태 | 언어 버전 설정 | 컴파일되는 기능 |
|--------|------|----------------|-----------------|
| 폴리필 없음 | 해당 없음 | 해당 없음 | 없음 |
| **xpTURN.Polyfill 0.4.0** | UPM, 공유 어셈블리 하나, public 타입 | 예: 빌드, 패키지, IDE | 전부 |
| [PolySharp](https://github.com/Sergio0694/PolySharp) 1.15.0, 1.16.0 | NuGet 소스 생성기, 어셈블리마다 internal 타입 | 아니요 | `[DynamicallyAccessedMembers]` 를 뺀 전부 |
| `PolySharpIncludeRuntimeSupportedAttributes` 를 켠 PolySharp 1.16.0 | 같음 | 아니요 | 없음(CS8336). `.globalconfig` 로 내장 특성까지 끄면 전부 |
| [PolyfillLib](https://github.com/SimonCropp/Polyfill) 11.4.1 | 의존 DLL 2개가 딸린 NuGet DLL, public 타입 | 아니요 | 전부 |
| [Polyfill](https://github.com/SimonCropp/Polyfill) 11.4.1 (소스 전용) | NuGet 소스 파일 | 아니요 | 없음: C# 14 문법 |
| [Meziantou.Polyfill](https://github.com/meziantou/Meziantou.Polyfill) 1.0.165 | NuGet 소스 생성기 | 아니요 | 없음: 중복 정의, 6000.6 에서는 CS8336 |
| [ModernCSharpForUnity](https://github.com/iAcolyte/ModernCSharpForUnity) 0.1.0 | UPM 소스 생성기 + Project Settings 페이지 | 예: `Assets` 의 asmdef 마다 `csc.rsp`, C# 9 ~ 12 | 6000.6 에서만 12개 중 9개. Roslyn 4.3 은 생성기를 싣지 않고(CS9057), 패키지가 Unity 6000.0 을 요구합니다 |
| [IsExternalInit](https://github.com/CorundumGames/IsExternalInit) 1.0.0 | OpenUPM DLL, public 타입 | 아니요 | `init`, `record`, `readonly record struct` |

이 패키지는 생성기 없는 평범한 C# 소스라서 Roslyn 버전에 기대지 않습니다. 그래서 설정 한 번으로 2022.3 부터 6000.6 까지 동작합니다.

다른 선택지가 나은 점:

- 생성기는 어셈블리마다 자기 `internal` 사본을 줍니다. asmdef 참조가 필요 없고 두 사본이 부딪히지 않습니다.
- PolySharp 와 PolyfillLib 는 트리밍 주석(`RequiresUnreferencedCode`, `DynamicDependency`) 같은 더 많은 타입을 주고, PolyfillLib 는 .NET Standard 2.1 에 없는 BCL 메서드도 더합니다. Unity 의 컴파일러와 IDE 는 트리밍 주석으로 아무것도 하지 않으므로 이 패키지는 이를 넣지 않았습니다.
- ModernCSharpForUnity 는 프로젝트마다 언어 버전과 nullable 설정을 고를 수 있게 합니다. 이 패키지는 `preview` 만 적용합니다.

이 패키지를 고르기 전에 알아야 할 한계:

- 타입이 공유 어셈블리에 `public` 으로 있으므로, 이 타입을 쓰는 asmdef 마다 `xpTURN.Polyfill.Runtime` 참조가 필요합니다.
- 같은 타입을 public 으로 내놓는 두 번째 어셈블리를 참조하지 마세요. 이 패키지와 public `IsExternalInit` 를 선언한 DLL 을 함께 쓰면 `init` 이 CS0518 로 실패합니다. 그때 Editor 가 그 어셈블리의 이름을 경고로 알려 줍니다.
- 위의 `.globalconfig` 로 두 옵션을 모두 설정하면 PolySharp 1.16.0 과 함께 컴파일됩니다.
- 런타임에 없는 것은 어떤 폴리필도 더할 수 없습니다: static abstract 인터페이스 멤버(제네릭 수학), `ref` 필드, 인라인 배열.

## 제공하는 타입

| C# 버전 | 기능 | 타입 |
|---------|------|------|
| C# 9  | `init` 전용 setter | [IsExternalInit](./src/Polyfill/Assets/Polyfill/Runtime/Init/IsExternalInit.cs) |
| C# 9  | 모듈 이니셜라이저 | [ModuleInitializerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Module/ModuleInitializerAttribute.cs) |
| C# 9  | 지역 변수 초기화 생략 | [SkipLocalsInitAttribute](./src/Polyfill/Assets/Polyfill/Runtime/SkipLocalsInit/SkipLocalsInitAttribute.cs) |
| C# 9  | 동적으로 접근하는 멤버 (트리머 분석) | [DynamicallyAccessedMembersAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/DynamicallyAccessedMembersAttribute.cs), [DynamicallyAccessedMemberTypes](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/DynamicallyAccessedMemberTypes.cs) |
| C# 9  | 메서드가 채우는 멤버의 nullable 분석 | [MemberNotNullAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/MemberNotNullAttribute.cs), [MemberNotNullWhenAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/MemberNotNullWhenAttribute.cs) |
| C# 10 | 커스텀 보간 문자열 핸들러 | [InterpolatedStringHandlerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerAttribute.cs), [InterpolatedStringHandlerArgumentAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerArgumentAttribute.cs) |
| C# 10 | 호출자 인자 식 | [CallerArgumentExpressionAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Caller/CallerArgumentExpressionAttribute.cs) |
| C# 11 | `required` 멤버 | [RequiredMemberAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/RequiredMemberAttribute.cs) |
| C# 11 | `required` 를 채우는 생성자 | [SetsRequiredMembersAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/SetsRequiredMembersAttribute.cs) |
| C# 11 | 컴파일러 기능 요구 | [CompilerFeatureRequiredAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Required/CompilerFeatureRequiredAttribute.cs) |
| C# 11 | 구조체 자신의 필드에 대한 `ref` (Unity 6000.5 이상, 이전 컴파일러는 무시) | [UnscopedRefAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/UnscopedRefAttribute.cs) |
| C# 12 | 사용자 타입의 컬렉션 식 (Unity 6000.5 이상) | [CollectionBuilderAttribute](./src/Polyfill/Assets/Polyfill/Runtime/Collections/CollectionBuilderAttribute.cs) |
| C# 12 | 실험적 API 진단 (Unity 6000.5 이상) | [ExperimentalAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/ExperimentalAttribute.cs) |
| IDE   | 문자열 인자의 언어 (정규식, JSON, 날짜 서식) | [StringSyntaxAttribute](./src/Polyfill/Assets/Polyfill/Runtime/CodeAnalysis/StringSyntaxAttribute.cs) |

각 타입은 대상 프레임워크에 그 타입이 없을 때만 컴파일되므로(`#if !NET5_0_OR_GREATER` 등), 런타임 자체 타입이 있으면 그쪽에 자리를 내줍니다.

### StringBuilder 헬퍼

`sb.AppendInterpolated($"…")` 는 문자열을 먼저 만들지 않고 `AppendInterpolatedStringHandler` 를 거쳐 붙입니다.

```csharp
using System.Globalization;
using System.Text;

var sb = new StringBuilder();
sb.AppendInterpolated($"frame={frame} t={t:F2} state={state}");
sb.AppendInterpolated(CultureInfo.InvariantCulture, $"{x:F3},{y:F3}");
```

정수, 부동소수점 수, `decimal`, `bool`, `char`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid` 는 스택 버퍼에 서식화되고, 서식이 없거나 `G` 인 열거형 값은 처음 쓸 때 만든 이름 표에서 씁니다. 이 구멍들은 할당하지 않지만, Unity 의 `TryFormat` 안에서 예외가 둘 있습니다: 서식이 `O` 나 `R` 이 아닌 `DateTime` 과 `DateTimeOffset`(기본 서식도 할당합니다), 그리고 `g` 서식의 `TimeSpan`. 그 밖의 타입, 열거형의 다른 서식, `ICustomFormatter` 를 돌려주는 provider, 1024자를 넘는 결과는 기존 `ToString` 경로를 탑니다.

| 작성한 코드 | Unity (이 패키지 사용) | .NET 6 이상 |
|-------------|------------------------|-------------|
| `sb.Append($"…")` | 문자열을 만든 뒤 `Append(string)` 호출 | BCL 핸들러 |
| `sb.AppendInterpolated($"…")` | 이 핸들러 | 이 패키지 없이는 컴파일되지 않음 |
| `sb.Append(provider, $"…")` | 이 핸들러 (확장 메서드) | BCL 핸들러 (인스턴스 메서드) |

`sb.Append($"…")` 는 핸들러를 쓸 수 없습니다. 이름이 같으면 인스턴스 메서드가 언제나 확장 메서드보다 우선하기 때문입니다. .NET 6 이상으로도 컴파일되는 코드에서는 `sb.Append(provider, $"…")` 가 양쪽에서 동작합니다.

## 요구 사항

- Unity 2022.3.12f1 이상
- Unity 2022.3.62f3, 6000.3.9f1, 6000.6.0f1 에서 [tools/run-compat.sh](./tools/run-compat.sh) 로 검증했습니다. 결과: [docs/Compatibility.md](./docs/Compatibility.md#version-matrix).

## 설치

1. **Window > Package Manager** 를 엽니다.
2. **+** > **Add package from git URL...** 을 누릅니다.
3. 다음을 입력합니다.

```text
https://github.com/xpTURN/Polyfill.git?path=src/Polyfill/Assets/Polyfill
```

특정 릴리스에 머물려면 태그를 붙입니다.

```text
https://github.com/xpTURN/Polyfill.git?path=src/Polyfill/Assets/Polyfill#v0.4.0
```

### 프로젝트 설정 (C# 언어 버전)

C# 9 보다 새로운 문법은 두 곳에 더 새로운 언어 버전이 필요합니다: 빌드를 위한 Unity 의 컴파일러 인자, 그리고 IDE 를 위한 생성된 `.csproj` 파일입니다. 메뉴 명령 하나가 둘 다 설정합니다.

**Edit > Polyfill > Player Settings > Apply Additional Compiler Arguments -langversion (All Installed Platforms)**

이 명령은:

- 설치된 모든 플랫폼의 Player 설정 **Additional Compiler Arguments** 에 `-langversion:preview` 를 더합니다. Unity 의 컴파일러가 이 값을 씁니다.
- 같은 플랫폼의 **Scripting Define Symbols** 에 `CSHARP_PREVIEW` 를 더해, 코드에서 `#if CSHARP_PREVIEW` 로 확인할 수 있게 합니다.
- `.csproj` 파일을 `<LangVersion>preview</LangVersion>` 로 다시 생성해 IDE(Visual Studio, Cursor, OmniSharp 등)가 같은 문법을 읽게 합니다. 이후에 다시 생성해도 유지됩니다.

이 선택은 `ProjectSettings/xpTURN.Polyfill.Settings.json` 에 저장됩니다. asmdef 옆에 [docs/csc.rsp](./docs/csc.rsp) 같은 `csc.rsp` 파일을 둘 필요가 없습니다. **Edit > Polyfill > Regenerate Project Files** 로 언제든 `.csproj` 파일을 다시 쓸 수 있습니다.

되돌리려면 **Edit > Polyfill > Player Settings > Remove Additional Compiler Arguments -langversion (All Installed Platforms)** 를 실행합니다.

### `preview` 가 받는 문법

`preview` 는 에디터에 든 컴파일러의 가장 새로운 언어 버전을 뜻합니다.

| Unity | 컴파일러 | `-langversion:preview` 에서 |
|-------|----------|-----------------------------|
| 2022.3, 6000.3 | Roslyn 4.3 | C# 10, 그리고 `file` 타입(CS0116)과 `[UnscopedRef]`(무시되므로 `ref` 반환이 CS8170 으로 실패)를 뺀 C# 11. C# 12 는 안 됩니다. |
| 6000.6 | Roslyn 4.10 | 인라인 배열을 뺀 C# 12, 그리고 C# 13 일부(`params` 스팬, `\e`) |

Unity 릴리스 노트에 따르면 6000.5 에서 컴파일러가 올라갑니다. Roslyn 4.10 이 필요한 코드(C# 12, `file` 타입, `[UnscopedRef]`)는 `#if CSHARP_PREVIEW && UNITY_6000_5_OR_NEWER` 로 감싸세요.

함정:

- 이 인자는 `Library/PackageCache` 의 서드파티 · Unity 패키지를 포함해 프로젝트의 모든 어셈블리에 적용됩니다.
- 제네릭 특성(`[MyAttribute<int>]`)은 Roslyn 4.3 에서 컴파일되지만, Unity 2022.3 과 6000.3 의 IL2CPP 빌드를 특성 이름을 알려 주지 않는 `InvalidCastException` 으로 깨뜨립니다. Unity 6000.6 은 빌드합니다.
- `ref` 필드는 Roslyn 4.3 에서 오류 없이 컴파일되지만 런타임이 지원하지 않습니다. Roslyn 4.10 은 CS9064 를 냅니다.
- static abstract 인터페이스 멤버(CS8919)와 메서드에 붙인 `[AsyncMethodBuilder]`(CS0592)는 세 에디터 어디서도 컴파일되지 않습니다.

### Assembly Definition 사용

프로젝트가 **Assembly Definition**(.asmdef)을 쓴다면, 폴리필 타입(`init`, `record`, `required`, 보간 문자열 핸들러 등)을 쓰는 모든 asmdef 에 이 패키지의 런타임 어셈블리 참조를 더하세요.

1. **Assembly Definition**(.asmdef) 파일을 선택해 Inspector 에서 엽니다.
2. **References** 에서 **+** 를 누르고 **xpTURN.Polyfill.Runtime** 을 더합니다.

이 참조가 없으면 그 어셈블리의 스크립트가 `IsExternalInit` 나 `RequiredMemberAttribute` 같은 타입을 보지 못해 컴파일이 실패할 수 있습니다.

패키지 참조를 더한 뒤의 모습:

<img src="./docs/assets/Assembly-Definition-References.png" alt="Assembly Definition References" width="420">

## 사용 예

### init 전용 속성 (C# 9)

```csharp
public class Data
{
    public string Id { get; init; }
    public int Value { get; init; }
}

var d = new Data { Id = "a", Value = 1 };
```

### record (C# 9)

`init` 폴리필 위에서 동작합니다. 값 동등성과 `with` 식을 지원합니다.

```csharp
public record Point(int X, int Y);

var p = new Point(1, 2);
var q = p with { Y = 3 };  // Point(1, 3)
```

### SkipLocalsInit (C# 9)

메서드의 지역 변수를 0 으로 채우지 않도록 컴파일러에 알려, 핫 패스의 큰 `stackalloc` 버퍼에서 시간을 아낍니다. 모든 원소를 읽기 전에 쓰는 곳에서만 쓰세요. 어셈블리가 unsafe 코드를 허용해야 하지만(asmdef 의 **Allow 'unsafe' Code**, 없으면 CS0227), 메서드 자체에는 `unsafe` 키워드가 필요 없습니다.

```csharp
using System;
using System.Runtime.CompilerServices;

[SkipLocalsInit]
static string ToHex(ReadOnlySpan<byte> data)
{
    Span<char> chars = data.Length <= 128 ? stackalloc char[data.Length * 2] : new char[data.Length * 2];
    for (int i = 0; i < data.Length; i++)
    {
        chars[i * 2] = "0123456789abcdef"[data[i] >> 4];      // 모든 글자를 읽기 전에 쓴다
        chars[i * 2 + 1] = "0123456789abcdef"[data[i] & 0xF];
    }
    return new string(chars);
}
```

### 모듈 이니셜라이저 (C# 9)

`[ModuleInitializer]` 메서드는 아무도 부르지 않아도 어셈블리마다 한 번 실행되므로, 소스 생성기가 만드는 등록 코드에 잘 맞습니다. `static` 이고, 매개변수가 없고, `void` 를 돌려주고, `internal` 이나 `public` 이어야 합니다(그렇지 않으면 CS8814, CS8815).

```csharp
using System.Runtime.CompilerServices;

static class CommandRegistration
{
    [ModuleInitializer]
    internal static void Register() => CommandRegistry.Add(typeof(MoveCommand));
}
```

메서드가 언제 실행되는지는 코드가 어디서 도는지에 달려 있습니다.

| 어디서 | 모듈 이니셜라이저가 실행되는 때 |
|--------|---------------------------------|
| 에디터 | 사용자 코드가 돌기 전에 모두 (`[InitializeOnLoad]` 보다도 먼저) |
| IL2CPP 플레이어 | 시작할 때 모두 |
| Mono 플레이어 | 어셈블리를 처음 쓸 때: 정적 메서드 호출, 정적 필드 읽기, 객체의 메서드 호출. `typeof` 만으로는 실행되지 않고, 아무도 건드리지 않는 어셈블리는 끝까지 실행되지 않습니다. |

- Mono 플레이어에서 다른 어셈블리의 등록에 기대는 코드는 그 어셈블리의 이니셜라이저를 먼저 실행하세요: `RuntimeHelpers.RunModuleConstructor(typeof(SomeTypeInThatAssembly).Module.ModuleHandle)`.
- IL2CPP 는 이 호출에서 `NotSupportedException` 을 던지므로 `try`/`catch` 로 감싸세요. IL2CPP 에서는 이니셜라이저가 이미 실행된 상태입니다.

### CallerArgumentExpression (C# 10)

컴파일러가 지정한 매개변수에 인자의 **소스 텍스트**를 넘깁니다. 단언이나 진단에 유용합니다.

```csharp
using System.Runtime.CompilerServices;

static void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string expression = null)
{
    if (!condition)
        throw new System.ArgumentException($"Condition failed: {expression}");
}

Assert(x > 0);  // 실패하면: "Condition failed: x > 0"
```

### 커스텀 보간 문자열 (C# 10)

[InterpolatedStringHandlerAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerAttribute.cs) 를 쓰면 메서드가 완성된 `string` 대신 직접 만든 구조체로 `$"…"` 를 받을 수 있습니다. 컴파일러가 리터럴을 그 구조체의 호출들로 바꾸므로, 텍스트가 생기기 전에 메서드가 무엇을 할지 정합니다. 우리 프로젝트는 세 가지로 씁니다.

| 쓰임 | `string` 매개변수로는 못 하는 것 | 쓰는 곳 |
|------|-----------------------------------|---------|
| 꺼진 로그 레벨 건너뛰기 | 레벨이 꺼져 있는 동안 텍스트를 만들지 않고 `{…}` 식도 실행하지 않으며, 릴리스 빌드에서 호출을 지운다 | [Klotho](https://github.com/xpTURN/Klotho) 로거, [XLogger](https://github.com/xpTURN/XLogger) |
| 재사용 버퍼에 쓰기 | 문자열을 만들지 않고 UI 텍스트를 갱신한다 | [XString](https://github.com/xpTURN/XString) `label.SetTextX($"…")` |
| 값에 이름 붙이기 | 구멍마다 그 식의 이름을 붙인 로그 필드로 만든다 | [ZLogger](https://github.com/Cysharp/ZLogger) 위의 XLogger |

#### 레벨이 꺼져 있으면 일을 건너뛴다

[InterpolatedStringHandlerArgumentAttribute](./src/Polyfill/Assets/Polyfill/Runtime/InterpolatedString/InterpolatedStringHandlerArgumentAttribute.cs) 가 로거를 핸들러 생성자에 넘기고, 생성자가 메시지를 만들지 답합니다.

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
    [System.Diagnostics.Conditional("DEBUG")]   // 에디터와 개발 빌드
    public static void Verbose(this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref VerboseHandler message)
    {
        if (message.Text is { } text) logger.Log(LogType.Log, text);
    }
}
```

호출은 한 줄 그대로입니다.

```csharp
Debug.unityLogger.Verbose($"tick={tick} state={DumpState()}");
```

컴파일러가 만드는 코드는 대략 다음과 같습니다.

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

- `Debug.unityLogger.filterLogType = LogType.Warning` 이면 이 호출은 텍스트를 만들지 않고 `DumpState()` 도 실행하지 않습니다. `Debug.Log($"…")` 는 Unity 가 필터를 확인하기 전에 문자열 전체를 만듭니다.
- 릴리스 플레이어는 `DEBUG` 를 정의하지 않으므로, 컴파일러가 호출을 핸들러와 인자까지 함께 지웁니다.
- Klotho 의 로거가 레벨마다 핸들러 하나를 둔 이 모양입니다. 핸들러가 숫자를 `TryFormat` 으로 스레드별 `char[]` 에 쓰므로, 숫자와 문자열이라면 켜진 호출도 최종 문자열 하나만 할당합니다.

#### 재사용 버퍼에 쓴다

[XString](https://github.com/xpTURN/XString) 은 ZString 의 풀 버퍼에 서식을 쓰고 그 문자들을 TextMesh Pro 에 넘기므로, 매 프레임 갱신하는 라벨도 문자열을 만들지 않습니다.

```csharp
hpLabel.SetTextX($"HP {hp} / {maxHp}");
```

내부를 간단히 하면:

```csharp
public static void SetTextX(this TMP_Text label, ref XStringHandler message)
{
    var chars = message.Builder.AsArraySegment();   // ZString Utf16ValueStringBuilder
    label.SetCharArray(chars.Array, chars.Offset, chars.Count);
    message.Builder.Dispose();                       // 풀에 돌려준다
}
```

#### 값에 이름을 붙인다

`[CallerArgumentExpression]` 은 핸들러의 `AppendFormatted` 에서도 동작하므로, 구멍마다 그 소스 텍스트가 함께 옵니다. XLogger 는 이를 ZLogger 에 넘기고, ZLogger 는 구조화 로그 필드로 씁니다.

```csharp
public void AppendFormatted<T>(T value, int alignment = 0, string format = null,
    [CallerArgumentExpression("value")] string argumentName = null)
    => _inner.AppendFormatted(value, alignment, format, argumentName);
```

```csharp
logger.XLogInformation($"Player {playerId} reached {score} points");   // argumentName: "playerId", "score"
```

#### 직접 만들 때

- 생성자는 `(int literalLength, int formattedCount)` 다음에 `[InterpolatedStringHandlerArgument]` 에 적은 매개변수를, 그다음에 필요하면 `out bool` 을 받습니다. `AppendLiteral(string)` 과, 받을 구멍 모양(`{x}`, `{x:F2}`, `{x,8}`)마다 `AppendFormatted` 를 더하세요.
- `ToString()` 을 부르는 제네릭 `AppendFormatted<T>(T value)` 는 값 형식 구멍마다 할당하고, `value is IFormattable` 은 박싱을 더합니다. 구멍마다 할당이 두 번이라, 그런 구멍이 둘이면 평범한 `$"…"` 보다 많아집니다. 로그에 쓰는 타입의 오버로드를 더해 Klotho 처럼 `TryFormat` 으로 서식화하거나, 샘플 [XHandler](./samples/PolyfillSample/Assets/Scripts/InterpolatedString/XHandler.cs) 처럼 구멍을 이 패키지의 `AppendInterpolatedStringHandler`([StringBuilder 헬퍼](#stringbuilder-헬퍼))에 넘기세요.
- 정적 필드에 둔 버퍼는 구멍 안에서 일어나는 중첩 호출을 견뎌야 합니다. Klotho 는 쓰는 동안 `[ThreadStatic]` 버퍼를 필드에서 꺼내 둡니다.
- 핸들러만 받는 메서드는 `$"…"` 만 받습니다. `logger.Verbose("tick=" + tick)` 는 CS1620(*must be passed with the 'ref' keyword*)으로 실패하는데, 이 메시지는 이유를 알려 주지 않습니다. 호출하는 쪽에 필요하면 `string` 오버로드를 더하세요.
- `using UnityEngine;` 과 함께라면 `[System.Diagnostics.Conditional(...)]` 를 전부 적으세요. `using System.Diagnostics;` 를 더하면 `Debug` 가 모호해집니다(CS0104).
- 바로 쓸 수 있는 것: 로깅은 [ZLogger](https://github.com/Cysharp/ZLogger) 와 [XLogger](https://github.com/xpTURN/XLogger), 문자열은 [ZString](https://github.com/Cysharp/ZString) 과 [XString](https://github.com/xpTURN/XString).

### required 멤버 (C# 11)

`required` 멤버는 객체 초기화에서 채워야 합니다. `[SetsRequiredMembers]` 를 붙인 생성자는 스스로 채웁니다.

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
var b = new Config("lobby");             // OK: 생성자가 Name 을 채운다
var c = new Config { Port = 9000 };      // CS9035: required member 'Config.Name' must be set
```

### Unity 의 MonoBehaviour 와 네임스페이스

MonoBehaviour 와 ScriptableObject 클래스는 블록 네임스페이스에 선언하세요. Unity 는 파일을 읽어 스크립트 파일과 클래스를 잇는데, Unity 2022.3, 6000.3, 6000.6 에서는 file-scoped 네임스페이스(`namespace Game;`)면 이 연결이 실패합니다. `MonoScript.GetClass()` 가 null 을 돌려주고, 컴포넌트를 붙이면 *"Can't add script component 'Player' because the script class cannot be found"* 가 나타납니다.

```csharp
using UnityEngine;

namespace Game
{
    public class Player : MonoBehaviour { }
}
```

핸들러나 로거 같은 다른 클래스는 file-scoped 네임스페이스를 써도 됩니다.

## 라이선스

xpTURN.Polyfill 코드는 Apache License, Version 2.0 을 따릅니다. 자세한 내용은 [LICENSE](./LICENSE) 를 보세요.

## 링크

- **변경 기록**: [CHANGELOG](./CHANGELOG.md)
- **샘플**: [samples/PolyfillSample](./samples/PolyfillSample) — 이 패키지를 경로로 참조하는 Unity 2022.3 프로젝트
- **라이선스**: [LICENSE](./LICENSE)
- **만든 이**: [xpTURN](https://github.com/xpTURN)
