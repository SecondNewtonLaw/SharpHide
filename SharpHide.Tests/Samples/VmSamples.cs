namespace SharpHide.Tests;

public static class VmSamples {
    [Virtualize]
    public static int Add(int a, int b) => unchecked(a + b);

    [Virtualize]
    public static int Subtract(int a, int b) => unchecked(a - b);

    [Virtualize]
    public static int Multiply(int a, int b) => unchecked(a * b);

    [Virtualize]
    public static int Negate(int value) => unchecked(-value);

    [Virtualize]
    public static int CheckedAdd(int a, int b) => checked(a + b);

    [Virtualize]
    public static int CheckedSub(int a, int b) => checked(a - b);

    [Virtualize]
    public static int CheckedMul(int a, int b) => checked(a * b);

    [Virtualize]
    public static uint CheckedUnsignedAdd(uint a, uint b) => checked(a + b);

    [Virtualize]
    public static uint CheckedUnsignedSub(uint a, uint b) => checked(a - b);

    [Virtualize]
    public static uint CheckedUnsignedMul(uint a, uint b) => checked(a * b);

    [Virtualize]
    public static int Divide(int a, int b) => a / b;

    [Virtualize]
    public static uint UnsignedDivide(uint a, uint b) => a / b;

    [Virtualize]
    public static int Remainder(int a, int b) => a % b;

    [Virtualize]
    public static uint UnsignedRemainder(uint a, uint b) => a % b;

    [Virtualize]
    public static int Shift(int a, int b) => (a << b) ^ (a >> b) ^ (a >>> b);

    [Virtualize]
    public static int SignedCompare(int a, int b) => a < b ? -1 : a == b ? 0 : 1;

    [Virtualize]
    public static int UnsignedCompare(uint a, uint b) => a < b ? -1 : a == b ? 0 : 1;

    [Virtualize]
    public static int Narrow(int a) => unchecked((sbyte)a + (byte)a + (short)a + (ushort)a);

    [Virtualize]
    public static int CheckedByte(int a) => checked((byte)a);

    [Virtualize]
    public static int CheckedSbyte(uint a) => checked((sbyte)a);

    [Virtualize]
    public static int CheckedShort(int a) => checked((short)a);

    [Virtualize]
    public static int CheckedUshort(uint a) => checked((ushort)a);

    [Virtualize]
    public static int CheckedInt(uint a) => checked((int)a);

    [Virtualize]
    public static uint CheckedUint(int a) => checked((uint)a);

    [Virtualize]
    public static int Reassign(int a, int b, int c, int d, int e, int f) {
        f += e;
        while (a > 0) {
            f ^= b + c * d;
            a--;
        }

        return f;
    }

    [Virtualize]
    public static int Select(int n) {
        switch (n) {
            case 0:
                return 13;
            case 1:
                return -20;
            case 2:
                return 400;
            case 3:
                return 7;
            case 4:
                return -333;
            case 5:
                return 62;
            default:
                return n + 9;
        }
    }
}
