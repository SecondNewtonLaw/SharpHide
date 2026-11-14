namespace SharpHide.Vm;

public delegate ref T VmReturn<T>(VmFrame frame) where T : allows ref struct;
public delegate ref byte VmRoot(VmFrame frame, int index);
