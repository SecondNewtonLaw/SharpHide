namespace SharpHide.Vendor;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor,
    Inherited = false)]
public sealed class VirtualizeAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor,
    Inherited = false)]
public sealed class DoNotVirtualizeAttribute : Attribute;
