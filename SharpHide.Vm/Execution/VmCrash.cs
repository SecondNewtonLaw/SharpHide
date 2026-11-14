using System.Diagnostics.CodeAnalysis;

namespace SharpHide.Vm;

public static class VmCrash {
    [ThreadStatic]
    private static bool integrityFault;

    public static bool Diagnostics = true;

    internal static bool ConsumeIntegrityFault() {
        var fault = integrityFault;
        integrityFault = false;
        return fault;
    }

    [DoesNotReturn]
    public static void Fail() {
        if (Diagnostics)
            throw IntegrityException("0");
        Environment.FailFast(null);
    }

    [DoesNotReturn]
    public static void Fail(int code) {
        if (Diagnostics)
            throw IntegrityException(code.ToString());
        Environment.FailFast(null);
    }

    private static BadImageFormatException IntegrityException(string message) {
        integrityFault = true;
        return new BadImageFormatException(message);
    }

    [DoesNotReturn]
    public static T Fail<T>() {
        Fail();
        return default!;
    }

    [DoesNotReturn]
    public static T Fail<T>(int code) {
        Fail(code);
        return default!;
    }
}
