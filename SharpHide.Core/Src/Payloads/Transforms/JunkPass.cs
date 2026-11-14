#pragma warning disable CS0219, CS8600, CS8605 // intentional dead dynamic junk
namespace SharpHide.Core.Payloads;

// Reversible no-op whose dead body expands into dynamic CallSite IL.
public static class JunkPass {
    public static void Bloat(byte[] buffer) {
        if (buffer.Length > 0 && buffer[0] == buffer[0] + 1) {
            dynamic a = buffer.Length;
            dynamic b = a + 7;
            dynamic c = a * b - 3;
            dynamic d = (a ^ b) + c;
            dynamic e = a % (b + 1);
            dynamic f = "" + a + b + c + d + e;
            dynamic g = d * e - a + b;
            buffer[0] = (byte)(int)(a + b + c + d + e + g + f.Length);
        }
    }
}
