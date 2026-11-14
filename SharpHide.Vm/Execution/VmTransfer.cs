namespace SharpHide.Vm;

public sealed class VmTransfer(VmExceptionState state) : Exception {
    public readonly VmExceptionState State = state;
}
