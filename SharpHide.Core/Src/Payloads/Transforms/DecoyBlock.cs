#pragma warning disable CS0219, CS8600, CS8603 // intentional dead dynamic junk
namespace SharpHide.Core.Payloads;

// Dead dynamic block. Guard stays zero but remains mutable to block constant folding.
public static class DecoyBlock {
    public static int Guard;
    public static object? Sink;

    public static void Run() {
        if (Guard != 0) {
            dynamic a = Guard;
            dynamic b = a + 7;
            dynamic c = a * b - 3;
            dynamic d = (a ^ b) + c;
            dynamic e = a % (b + 1);
            Sink = a + b + c + d + e;
        }
    }
}
