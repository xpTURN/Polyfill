#if UNITY_6000_5_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compat
{
    file class FileLocal { public int V => 7; }

    [CollectionBuilder(typeof(BagBuilder), nameof(BagBuilder.Create))]
    public sealed class Bag : IEnumerable<int>
    {
        public int[] Items;
        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)Items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
    }
    public static class BagBuilder { public static Bag Create(ReadOnlySpan<int> s) => new Bag { Items = s.ToArray() }; }

    public struct Cell { int _v; [UnscopedRef] public ref int V => ref _v; public int Read => _v; }
    public class Pc(int x) { public int X => x; }

    public static class SmokeCs12
    {
        [Experimental("COMPAT001")] static int Exp() => 11;

        public static void Run(Action<string, string> log)
        {
            Smoke.Check(log, "cs11:file-type", () => new FileLocal().V == 7);
            Smoke.Check(log, "cs11:unscoped-ref", () => { var c = new Cell(); c.V = 5; return c.Read == 5; });
            Smoke.Check(log, "cs12:collection-expression", () => { int[] a = [1, 2, 3]; List<int> l = [.. a, 4]; ReadOnlySpan<int> s = [5, 6]; return a.Length == 3 && l.Count == 4 && l[3] == 4 && s[1] == 6; });
            Smoke.Check(log, "cs12:collection-builder", () => { Bag b = [1, 2, 3]; return b.Items.Length == 3 && b.Items[2] == 3; });
            Smoke.Check(log, "cs12:primary-constructor", () => new Pc(9).X == 9);
#pragma warning disable COMPAT001
            Smoke.Check(log, "cs12:experimental", () => Exp() == 11);
#pragma warning restore COMPAT001
        }
    }
}
#endif
