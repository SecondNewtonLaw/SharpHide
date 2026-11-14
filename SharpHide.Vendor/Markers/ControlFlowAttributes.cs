namespace SharpHide.Vendor;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor,
    Inherited = false)]
public sealed class ControlFlowAttribute : Attribute {
    public int MinBlocks { get; set; }
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
    public int BogusStates { get; set; }
    public int OpaqueDepth { get; set; }
    public double DecryptHashChance { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor,
    Inherited = false)]
public sealed class DoNotControlFlowAttribute : Attribute;
