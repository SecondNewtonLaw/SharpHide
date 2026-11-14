namespace SharpHide.Tests;

public static class VmExceptionSamples {
    public static int Trace;
    private static bool Step(int value) {
        Trace = Trace * 10 + value;
        return true;
    }

    [Virtualize]
    public static int Nested(int value) {
        Trace = 0;
        try {
            try {
                Step(1);
                return 100 / value;
            }
            finally {
                Step(3);
            }
        }
        catch (DivideByZeroException) when (Step(2)) {
            Step(4);
            return Trace;
        }
        finally {
            Step(5);
        }
    }

    [Virtualize]
    public static void Callee() {
        try {
            Step(1);
            throw new InvalidOperationException("original");
        }
        finally {
            Step(3);
        }
    }

    [Virtualize]
    public static int Caller() {
        Trace = 0;
        try {
            Callee();
        }
        catch (InvalidOperationException) when (Step(2)) {
            Step(4);
        }
        return Trace;
    }

    public static int NativeCaller() {
        Trace = 0;
        try {
            Callee();
        }
        catch (InvalidOperationException) when (Step(2)) {
            Step(4);
        }
        return Trace;
    }

    private static bool BadFilter() {
        Step(2);
        throw new FormatException();
    }

    [Virtualize]
    public static int FilterThrows() {
        Trace = 0;
        try {
            Callee();
        }
        catch (InvalidOperationException) when (BadFilter()) {
            Step(9);
        }
        catch (InvalidOperationException) when (Step(4)) {
            Step(5);
        }
        return Trace;
    }

    [Virtualize]
    public static int FinallyThrows() {
        Trace = 0;
        try {
            try {
                Step(1);
                throw new InvalidOperationException();
            }
            finally {
                Step(2);
                throw new ArgumentException();
            }
        }
        catch (ArgumentException) when (Step(3)) {
            Step(4);
        }
        return Trace;
    }

    [Virtualize]
    public static int CatchInsideFinally() {
        Trace = 0;
        try {
            try {
                Step(1);
                throw new InvalidOperationException();
            }
            finally {
                try {
                    Step(3);
                    throw new ArgumentException();
                }
                catch (ArgumentException) {
                    Step(4);
                }
                Step(5);
            }
        }
        catch (InvalidOperationException) when (Step(2)) {
            Step(6);
        }
        return Trace;
    }

    [Virtualize]
    public static int Rethrow(Exception error) {
        Trace = 0;
        try {
            try {
                throw error;
            }
            catch {
                Step(1);
                throw;
            }
            finally {
                Step(2);
            }
        }
        catch (Exception actual) {
            return ReferenceEquals(actual, error) ? Trace : -1;
        }
    }
}
