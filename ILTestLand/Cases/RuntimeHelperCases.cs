using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class RuntimeHelperCases
{
    public static bool Case2001() { var value = RuntimeHelpers.GetHashCode(new object()); return value != 0; }
    public static bool Case2002() { var value = RuntimeHelpers.IsReferenceOrContainsReferences<int>(); return !value; }
    public static bool Case2003() { var value = RuntimeHelpers.IsReferenceOrContainsReferences<string>(); return value; }
    public static bool Case2004() { var value = RuntimeHelpers.IsReferenceOrContainsReferences<Pair>(); return !value; }
    public static bool Case2005() { var value = RuntimeHelpers.IsReferenceOrContainsReferences<PairRef>(); return value; }
    public static bool Case2006() { var value = RuntimeHelpers.GetSubArray(new[] { 1, 2, 3, 4 }, 1..3); return value.SequenceEqual(new[] { 2, 3 }); }
    public static bool Case2007() { var value = RuntimeHelpers.GetSubArray(new[] { 1, 2, 3, 4 }, ^3..); return value.SequenceEqual(new[] { 2, 3, 4 }); }
    public static bool Case2008() { var value = RuntimeHelpers.GetSubArray(new[] { 1, 2, 3, 4 }, ..^1); return value.SequenceEqual(new[] { 1, 2, 3 }); }
    public static bool Case2009() { var value = RuntimeHelpers.GetSubArray(new[] { 1, 2, 3, 4 }, ..); return value.Length == 4; }
    public static bool Case2010() { var value = RuntimeHelpers.GetHashCode("2010"); return value != 0; }
    public static bool Case2011() { var value = RuntimeHelpers.OffsetToStringData; return value >= 0; }
    public static bool Case2012() { var value = Marshal.SizeOf<Pair>(); return value == 8; }
    public static bool Case2013() { var value = Marshal.SizeOf<PairRef>(); return value == IntPtr.Size; }
    public static bool Case2014() { var value = Marshal.SizeOf(typeof(Pair)); return value == 8; }
    public static bool Case2015() { var value = Marshal.OffsetOf<Pair>(nameof(Pair.Right)); return value == 4; }
    public static bool Case2016() { var value = Marshal.OffsetOf<PairRef>(nameof(PairRef.Text)); return value == 0; }
    public static bool Case2017() { var value = typeof(Pair).IsLayoutSequential; return value; }
    public static bool Case2018() { var value = typeof(PairRef).IsLayoutSequential; return value; }
    public static bool Case2019() { var value = typeof(Pair).IsValueType; return value; }
    public static bool Case2020() { var value = typeof(PairRef).IsValueType; return !value; }
    public static bool Case2021() { var value = typeof(string).IsSealed; return value; }
    public static bool Case2022() { var value = typeof(Exception).IsClass; return value; }
    public static bool Case2023() { var value = typeof(IDisposable).IsInterface; return value; }
    public static bool Case2024() { var value = typeof(int).IsPrimitive; return value; }
    public static bool Case2025() { var value = typeof(decimal).IsPrimitive; return !value; }
    public static bool Case2026() { var value = typeof(int).IsEnum; return !value; }
    public static bool Case2027() { var value = typeof(Status).IsEnum; return value; }
    public static bool Case2028() { var value = typeof(List<int>).ContainsGenericParameters; return !value; }
    public static bool Case2029() { var value = typeof(List<>).ContainsGenericParameters; return value; }
    public static bool Case2030() { var value = typeof(List<int>).GetGenericTypeDefinition(); return value == typeof(List<>); }
    public static bool Case2031() { var value = typeof(Dictionary<string, int>).GetGenericArguments(); return value.SequenceEqual(new[] { typeof(string), typeof(int) }); }
    public static bool Case2032() { var value = typeof(Pair).GetMembers().Length; return value > 0; }
    public static bool Case2033() { var value = typeof(Pair).GetField(nameof(Pair.Left))!; return value.IsPublic; }
    public static bool Case2034() { var value = typeof(PropertyPair).GetFields(); return value.Any(field => field.Name == nameof(PropertyPair.Left)); }
    public static bool Case2035() { var value = typeof(PropertyPair).GetField(nameof(PropertyPair.Left))!.IsPublic; return value; }
    public static bool Case2036() { var value = typeof(RuntimeHelperCases).GetNestedTypes(BindingFlags.NonPublic); return value.Length > 0; }
    public static bool Case2037() { var value = typeof(RuntimeHelperCases).GetNestedTypes(BindingFlags.NonPublic); return value.Length >= 2; }
    public static bool Case2038() { var value = typeof(RuntimeHelperCases).Namespace; return value == null || value.Length >= 0; }
    public static bool Case2039() { var value = typeof(RuntimeHelperCases).AssemblyQualifiedName; return value!.Contains("ILTestLand", StringComparison.Ordinal); }
    public static bool Case2040() { var value = typeof(RuntimeHelperCases).FullName; return !string.IsNullOrEmpty(value); }
    public static bool Case2041() { var value = typeof(RuntimeHelperCases).Name; return !string.IsNullOrEmpty(value); }
    public static bool Case2042() { var value = typeof(RuntimeHelperCases).Module.ScopeName; return value.Length > 0; }
    public static bool Case2043() { var value = typeof(RuntimeHelperCases).Assembly.GetReferencedAssemblies(); return value.Any(name => name.Name == "System.Runtime"); }
    public static bool Case2044() { var value = typeof(RuntimeHelperCases).Assembly.GetName().Name; return value == "ILTestLand"; }
    public static bool Case2045() { var value = typeof(RuntimeHelperCases).Assembly.GetName().GetPublicKeyToken(); return value != null; }
    public static bool Case2046() { var value = RuntimeFeature.IsDynamicCodeSupported; return value; }
    public static bool Case2047() { var value = RuntimeFeature.IsDynamicCodeCompiled; return value; }
    public static bool Case2048() { var value = RuntimeFeature.IsSupported("IsDynamicCodeCompiled"); return value == RuntimeFeature.IsDynamicCodeCompiled; }
    public static bool Case2049() { var value = GC.GetGCMemoryInfo().HeapSizeBytes; return value >= 0; }
    public static bool Case2050() { var value = GC.GetTotalMemory(false); return value > 0; }

    [StructLayout(LayoutKind.Sequential)] private struct Pair { public int Left; public int Right; }
    [StructLayout(LayoutKind.Sequential)] private sealed class PairRef { public string Text = "x"; }
    private struct PropertyPair { public int Left; public int Right; }
    private enum Status { Zero, One }
}
