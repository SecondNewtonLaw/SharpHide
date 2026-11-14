namespace SharpHide.Vm;

public sealed class VmUnwind(VmEvaluation? resume, VmExceptionRegion[] regions, VmExceptionState? error) {
    public readonly VmEvaluation? Resume = resume;
    public readonly VmExceptionRegion[] Regions = regions;
    public readonly VmExceptionState? Error = error;
    public int Next;
}
