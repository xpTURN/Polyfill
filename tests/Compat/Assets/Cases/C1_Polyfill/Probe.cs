using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

public class P05_Init { public int X { get; init; } }
public record P06_Record(int X, int Y);
public readonly record struct P07_RecordStruct(int X);
public class P08_Required { public required string Name { get; set; } }
public class P09_SetsRequired { public required int A { get; set; } [SetsRequiredMembers] public P09_SetsRequired() { A = 1; } }
[InterpolatedStringHandler] public ref struct P10_Handler { public P10_Handler(int l, int f) { } public void AppendLiteral(string s) { } public void AppendFormatted<T>(T v) { } }
public static class P11_HandlerArg { public static void Log(object ctx, [InterpolatedStringHandlerArgument("ctx")] ref P10_Handler h) { } }
public static class P12_Caller { public static void Check(bool c, [CallerArgumentExpression("c")] string e = null) { } }
public static class P13_Skip { [SkipLocalsInit] public static unsafe void M() { Span<byte> b = stackalloc byte[16]; b[0] = 1; } }
public static class P14_Module { [ModuleInitializer] internal static void Init() { } }
public static class P15_Dam { public static void M<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() { } }
#if UNITY_6000_5_OR_NEWER
public struct P16_Unscoped { int _v; [UnscopedRef] public ref int V => ref _v; }
#endif
public static class P17_Lang { public static string Raw() => """raw"""; public static bool List(int[] a) => a is [1, .., 3]; public static ReadOnlySpan<byte> U8() => "abc"u8; }
