using System.Runtime.CompilerServices;

namespace SharpHide.Vendor;

public static class ObfuscationMacros {
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void VMBegin() { }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void VMEnd() { }
}
