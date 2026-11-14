namespace SharpHide.Vm;

public static class VmOpcodes {
    public const int Magic = 0x364D5653;
    public const int HeaderSize = 276;
    public const int MaxFrameSize = ushort.MaxValue;

    public const byte Ldarg = 1;
    public const byte Ldloc = 2;
    public const byte Stloc = 3;
    public const byte Ldc = 4;
    public const byte Dup = 5;
    public const byte Pop = 6;
    public const byte Starg = 7;
    public const byte Ldarga = 8;
    public const byte Ldloca = 9;

    public const byte Br = 40;
    public const byte BrTrue = 41;
    public const byte BrFalse = 42;
    public const byte Switch = 43;
    public const byte Leave = 45;
    public const byte Endfinally = 46;
    public const byte Endfilter = 47;
    public const byte Rethrow = 48;

    public const byte Ret = 50;
    public const byte Operation = 61;
    public const byte Localloc = 62;

    public const byte BitAnd = 80;
    public const byte BitOr = 81;
    public const byte BitXor = 82;
    public const byte ShiftLeft = 83;
    public const byte ShiftRight = 84;
    public const byte ShiftRightUnsigned = 85;
    public const byte BitNot = 86;

    public const byte Add = 90;
    public const byte Subtract = 91;
    public const byte Multiply = 92;
    public const byte Divide = 93;
    public const byte Remainder = 94;
    public const byte Negate = 95;
    public const byte AddChecked = 96;
    public const byte SubtractChecked = 97;
    public const byte MultiplyChecked = 98;
    public const byte AddUnsignedChecked = 99;
    public const byte SubtractUnsignedChecked = 100;
    public const byte MultiplyUnsignedChecked = 101;
    public const byte DivideUnsigned = 102;
    public const byte RemainderUnsigned = 103;
}
