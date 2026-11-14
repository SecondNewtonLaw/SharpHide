using System.Runtime.InteropServices;

namespace SharpHide.Core.Payloads;

public static partial class AntiDump {
    private const uint PageReadWrite = 0b100;

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualProtect(IntPtr address, ulong size, uint protect, out uint old);

    public static void Protect() {
        if (!OperatingSystem.IsWindows())
            return;

        try {
            /*
             *  would probably be best to assume the module isn't the main module, because of this we cannot walk the PEB and TEB using Ldr to hide anti-dump.
             */

            var handle =
                Marshal.GetHINSTANCE(typeof(AntiDump)
                    .Module); // GetModuleHandle(modulefnaddr) -> should return the nearest module.
            if (handle == IntPtr.Zero /* ??? */ || handle == new IntPtr(-1) /* in-memory image */)
                return;

            if (!VirtualProtect(handle, 2 /* MZ header size */, PageReadWrite, out uint val)) return;

            Marshal.WriteInt16(handle, 0, 0); // write off MZ into 0000.

            VirtualProtect(handle, 2 /* MZ header size */, val, out _); /* restore protection back */
        }
        catch {
            // ignored
        }
    }

    public static void Neutralize(byte[] image) { // test fn
        if (image.Length < 2)
            return;

        image[0] = 0; // remove M
        image[1] = 0; // remove Z
    }
}