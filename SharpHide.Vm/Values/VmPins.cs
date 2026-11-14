using System.Runtime.InteropServices;

namespace SharpHide.Vm;

public sealed class VmPins : IDisposable {
    private readonly Dictionary<object, PinnedGCHandle<object>> _handles = new(ReferenceEqualityComparer.Instance);

    public void Pin(VmReference reference) {
        object? owner = reference;
        while (owner is VmReference current) {
            if (current.IsCell && !current.IsPinned)
                VmCrash.Fail();
            owner = current.Owner;
        }
        if (owner is null or IntPtr || _handles.ContainsKey(owner))
            return;
        _handles.Add(owner, new PinnedGCHandle<object>(owner));
    }

    public void Dispose() {
        foreach (var handle in _handles.Values)
            handle.Dispose();
        _handles.Clear();
    }
}
