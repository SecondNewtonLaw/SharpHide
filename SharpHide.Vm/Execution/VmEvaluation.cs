namespace SharpHide.Vm;

public sealed class VmEvaluation(int position, int start, int end, byte stop, int size, int level) {
    public int Position = position;
    public int Origin = position;
    public readonly int Start = start;
    public readonly int End = end;
    public readonly byte Stop = stop;
    public readonly int Level = level;
    public readonly VmValue[] Registers = new VmValue[size];
    public bool Complete;
    public object? Result;
}
