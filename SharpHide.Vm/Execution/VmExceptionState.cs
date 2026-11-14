using System.Runtime.ExceptionServices;

namespace SharpHide.Vm;

public sealed class VmExceptionState(Exception error, int origin, VmExceptionRegion? handler) {
    public readonly ExceptionDispatchInfo Error = ExceptionDispatchInfo.Capture(error);
    public readonly int Origin = origin;
    public readonly VmExceptionRegion? Handler = handler;
    public readonly HashSet<VmExceptionRegion> Unwound = new();
}
