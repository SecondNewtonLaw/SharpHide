namespace SharpHide.Vm;

// Typed CLR locals keep embedded references tracked by the GC.
public sealed class VmNativeValue {
    private readonly Action<IntPtr, IntPtr> _copy;

    public VmNativeValue(IntPtr address, Type type, int bank, Action<IntPtr, IntPtr> copy) {
        Address = address;
        Type = type;
        Bank = bank;
        _copy = copy;
    }


    public readonly IntPtr Address;
    public readonly Type Type;
    public readonly int Bank;
    public Action<IntPtr, IntPtr> Copy => _copy;

    public void CopyTo(IntPtr destination) => _copy(Address, destination);
}
