using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpHide.Tests;

public static unsafe class VmIndirectSamples {
    public static int Add(int a, int b) => unchecked(a * 17 + b);

    public static int Increment(ref int value) => ++value;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int NativeAdd(int a, int b) => unchecked(a * 17 + b);

    [Virtualize]
    public static int ManagedPointer(int value) {
        delegate* managed<int, int, int> function = &Add;
        return function(value, 9);
    }

    [Virtualize]
    public static int UnmanagedPointer(int value) {
        delegate* unmanaged[Cdecl]<int, int, int> function = &NativeAdd;
        return function(value, 9);
    }

    [Virtualize]
    public static int NativeIdentity() {
        var windows = OperatingSystem.IsWindows();
        var library = NativeLibrary.Load(windows ? "kernel32.dll" : "libc.so.6");
        try {
            if (windows) {
                var function = (delegate* unmanaged[Stdcall, SuppressGCTransition]<int>)NativeLibrary.GetExport(library, "GetCurrentThreadId");
                return function();
            }
            else {
                var function = (delegate* unmanaged[Cdecl, SuppressGCTransition]<int>)NativeLibrary.GetExport(library, "getpid");
                return function();
            }
        }
        finally {
            NativeLibrary.Free(library);
        }
    }

    [Virtualize]
    private static int* Next(int* value) => value + 1;

    [Virtualize]
    private static void Repoint(ref int* value) => value++;

    [Virtualize]
    public static int PointerValues(int value) {
        var memory = (int*)Marshal.AllocHGlobal(8);
        try {
            *memory = value;
            *Next(memory) = value * 2;
            var pointers = new int*[1];
            pointers[0] = memory;
            Repoint(ref pointers[0]);
            return *memory + *pointers[0];
        }
        finally {
            Marshal.FreeHGlobal((IntPtr)memory);
        }
    }

    [Virtualize]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2014", Justification = "At most eight allocations; their shared invocation lifetime is under test.")]
    public static int StackMemory(int value) {
        var locations = new nint[1 + (value & 7)];
        var result = 0;
        try {
            for (var i = 0; i < locations.Length; i++) {
                int* memory = stackalloc int[i + 1];
                locations[i] = (nint)memory;
                for (var j = 0; j <= i; j++)
                    memory[j] = value + i * 17 + j;
            }
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            ref var first = ref Unsafe.AsRef<int>((void*)locations[0]);
            first += 3;
            if (value < 0)
                throw new InvalidOperationException();
        }
        catch (InvalidOperationException) {
            result = 29;
        }
        finally {
            for (var i = 0; i < locations.Length; i++)
                if (locations[i] != 0)
                    for (var j = 0; j <= i; j++)
                        result += ((int*)locations[i])[j];
        }
        return result;
    }

    [Virtualize]
    public static int FixedStorage(int value) {
        var state = new VmPinnedState { Value = value, Text = "root" };
        fixed (int* pointer = &state.Value) {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            *pointer += 5;
        }
        return state.Value + state.Text.Length;
    }

    [Virtualize]
    public static int FixedArray(int value) {
        var array = new[] { value };
        fixed (int* pointer = array) {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            *pointer += 7;
        }
        return array[0];
    }
}

public class VmPinnedState {
    public int Value;
    public string Text = "";
}
