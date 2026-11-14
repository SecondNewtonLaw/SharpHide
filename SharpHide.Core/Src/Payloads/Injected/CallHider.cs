using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpHide.Core.Payloads;

public static class CallHider {
    public static int Encode(int metadataToken, int seed) => metadataToken ^ seed;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object? Invoke(int encodedToken, int seed, object? target, object[] arguments) {
        var token = encodedToken ^ seed;
        var method = MethodBase.GetCurrentMethod()!.Module.ResolveMethod(token)!;
        return method.Invoke(target, BindingFlags.DoNotWrapExceptions, null, arguments, null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Delegate CreateDelegate(object? target, int encodedToken, int seed, Type delegateType) {
        var token = encodedToken ^ seed;
        var method = (MethodInfo)MethodBase.GetCurrentMethod()!.Module.ResolveMethod(token)!;
        return target is null
            ? Delegate.CreateDelegate(delegateType, method)
            : Delegate.CreateDelegate(delegateType, target, method);
    }
}
