namespace SharpHide.Vm;

public sealed class VmExceptionRegion {
    public int TryStart;
    public int TryEnd;
    public int HandlerStart;
    public int HandlerEnd;
    public int FilterStart;
    public int Kind;
    public Type? CatchType;
}
