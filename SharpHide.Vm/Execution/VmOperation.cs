namespace SharpHide.Vm;

public sealed class VmOperation(Type resultType, int resultBank, Func<object?[], IntPtr, object?> bridge,
    Action<IntPtr, IntPtr>? nativeCopy = null) {
    public readonly Type ResultType = resultType;
    public readonly int ResultBank = resultBank;
    public readonly Func<object?[], IntPtr, object?> Bridge = bridge;
    public readonly Action<IntPtr, IntPtr>? NativeCopy = nativeCopy;
}
