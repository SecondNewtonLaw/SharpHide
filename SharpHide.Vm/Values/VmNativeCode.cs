using System.ComponentModel;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SharpHide.Vm;

public sealed class VmNativeCode : SafeHandleZeroOrMinusOneIsInvalid {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ExecuteThunk(IntPtr buffer);

    private readonly ExecuteThunk _execute;
    private static readonly Dictionary<(short Code, bool Wide), VmNativeCode> Cache = new();

    private VmNativeCode(byte[] code) : base(true) {
        SetHandle(VirtualAlloc(IntPtr.Zero, (nuint)code.Length, 0x3000, 0x04));
        if (IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error());
        try {
            Marshal.Copy(code, 0, handle, code.Length);
            if (!VirtualProtect(handle, (nuint)code.Length, 0x20, out _) ||
                !FlushInstructionCache(new IntPtr(-1), handle, (nuint)code.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            _execute = Marshal.GetDelegateForFunctionPointer<ExecuteThunk>(handle);
        }
        catch {
            Dispose();
            throw;
        }
    }

    protected override bool ReleaseHandle() => VirtualFree(handle, 0, 0x8000);

    public static bool IsSupported(OpCode op) => op.Name is "add" or "sub" or "mul" or "and" or "or" or "xor" or
        "neg" or "not" or "shl" or "shr" or "shr.un" or "ceq" or "cgt" or "cgt.un" or "clt" or "clt.un";

    public static unsafe bool TryExecute(OpCode op, VmValue[] inputs, out VmValue result) {
        result = default;
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture is not (Architecture.X86 or Architecture.X64) ||
            !IsSupported(op) || inputs.Length is < 1 or > 2)
            return false;
        var wide = inputs[0].Type == typeof(long);
        if (!wide && inputs[0].Type != typeof(int) || wide && RuntimeInformation.ProcessArchitecture != Architecture.X64)
            return false;
        var unary = op == OpCodes.Neg || op == OpCodes.Not;
        var shift = op == OpCodes.Shl || op == OpCodes.Shr || op == OpCodes.Shr_Un;
        if (inputs.Length != (unary ? 1 : 2) ||
            !unary && inputs[1].Type != (shift ? typeof(int) : inputs[0].Type))
            return false;
        VmNativeCode code;
        lock (Cache) {
            var key = (op.Value, wide);
            if (!Cache.TryGetValue(key, out code!)) {
                code = new VmNativeCode(Emit(op, wide, RuntimeInformation.ProcessArchitecture == Architecture.X64));
                Cache.Add(key, code);
            }
        }
        long* buffer = stackalloc long[3];
        buffer[0] = wide ? (long)inputs[0].Value! : (int)inputs[0].Value!;
        buffer[1] = unary ? 0 : inputs[1].Type == typeof(long) ? (long)inputs[1].Value! : (int)inputs[1].Value!;
        buffer[2] = 0;
        code._execute((IntPtr)buffer);
        GC.KeepAlive(code);
        var comparison = op == OpCodes.Ceq || op == OpCodes.Cgt || op == OpCodes.Cgt_Un || op == OpCodes.Clt || op == OpCodes.Clt_Un;
        result = wide && !comparison ? VmValue.Load(buffer[2], typeof(long)) : VmValue.Load((int)buffer[2], typeof(int));
        return true;
    }

    public static byte[] Emit(OpCode op, bool wide, bool x64) {
        if (!IsSupported(op) || wide && !x64)
            throw new NotSupportedException("Unsupported native VM operation.");
        var code = new List<byte>();
        if (x64) {
            // RCX carries the buffer; R8 preserves it while ECX carries the shift count.
            code.AddRange([0x49, 0x89, 0xC8]);
            code.AddRange([wide ? (byte)0x49 : (byte)0x41, 0x8B, 0x00]);
            code.AddRange([wide ? (byte)0x49 : (byte)0x41, 0x8B, 0x48, 0x08]);
        }
        else
            code.AddRange([0x8B, 0x54, 0x24, 0x04, 0x8B, 0x02, 0x8B, 0x4A, 0x08]);
        if (wide)
            code.Add(0x48);
        var comparison = false;
        switch (op.Name) {
            case "add": code.AddRange([0x01, 0xC8]); break;
            case "sub": code.AddRange([0x29, 0xC8]); break;
            case "mul": code.AddRange([0x0F, 0xAF, 0xC1]); break;
            case "and": code.AddRange([0x21, 0xC8]); break;
            case "or": code.AddRange([0x09, 0xC8]); break;
            case "xor": code.AddRange([0x31, 0xC8]); break;
            case "neg": code.AddRange([0xF7, 0xD8]); break;
            case "not": code.AddRange([0xF7, 0xD0]); break;
            case "shl": code.AddRange([0xD3, 0xE0]); break;
            case "shr": code.AddRange([0xD3, 0xF8]); break;
            case "shr.un": code.AddRange([0xD3, 0xE8]); break;
            default:
                comparison = true;
                code.AddRange([0x39, 0xC8, 0x0F, op.Name switch {
                    "ceq" => 0x94, "cgt" => 0x9F, "cgt.un" => 0x97, "clt" => 0x9C, _ => (byte)0x92,
                }, 0xC0, 0x0F, 0xB6, 0xC0]);
                break;
        }
        if (x64)
            code.AddRange([wide && !comparison ? (byte)0x49 : (byte)0x41, 0x89, 0x40, 0x10]);
        else
            code.AddRange([0x89, 0x42, 0x10]);
        code.Add(0xC3);
        return code.ToArray();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAlloc(IntPtr address, nuint size, uint allocationType, uint protect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualProtect(IntPtr address, nuint size, uint protect, out uint previous);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFree(IntPtr address, nuint size, uint freeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, nuint size);
}
