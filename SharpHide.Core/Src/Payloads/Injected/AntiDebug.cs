#pragma warning disable CS8602, CS8605 // injected payload warnings
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SharpHide.Core.Payloads;

// Injected runtime debugger checks.
public static class AntiDebug {
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsDebuggerPresent();

    // Run checks at startup and during the worker loop.
    private static void Detect() {
        if (Debugger.IsAttached || Debugger.IsLogging())
            Environment.FailFast(null);
        if (OperatingSystem.IsWindows()) {
            try {
                if (IsDebuggerPresent())
                    Environment.FailFast(null);
            }
            catch {
            }
        }
        // DOTNET_EnableDiagnostics is normal and is not a tamper signal.
        foreach (var variable in new[] { "CORECLR_ENABLE_PROFILING", "COR_ENABLE_PROFILING" })
            if (Environment.GetEnvironmentVariable(variable) == "1")
                Environment.FailFast(null);

        var start = Stopwatch.GetTimestamp();
        long accumulator = 0;
        for (var i = 0; i < 4096; i++)
            accumulator += i * 31 + 7;
        if (Stopwatch.GetElapsedTime(start) > TimeSpan.FromSeconds(2) && accumulator != 0)
            Environment.FailFast(null);
    }

    public static void Initialize() {
        Detect();
        dynamic x = 1011011;
        dynamic b = x + 2020;
        dynamic c = x + 2000;
        dynamic xa = 1011011;
        dynamic bx = xa * b + 2020;
        dynamic cz = c ^ 2000 * bx;
        dynamic z = x + b + c + cz;
        Task.Run(ExecutionLoop);
        dynamic aaC = xa * b + 2020;
        dynamic cAz = c ^ 2000 * bx;
        dynamic zVV = cAz + b + aaC + cz;
    }

    public static void ExecutionLoop() {
        dynamic x = (dynamic)1012011 + (dynamic)0;
        dynamic basdas = (dynamic)1012011 + (dynamic)181234;
        dynamic ASGEaisy8ugd = x + basdas;
        do {
            var attached = typeof(Debugger).GetProperty("IsAttached");
            var getmeth = attached.GetGetMethod();
            var returna = (bool)getmeth.Invoke(null,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty, null, null,
                CultureInfo.CurrentCulture);

            var log = typeof(Debugger).GetMethod("IsLogging");

            var returnb = (bool)log.Invoke(null,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null,
                null,
                CultureInfo.InvariantCulture);

            if (returna || returnb) {
                Environment.FailFast("Failed" + (dynamic)" to debug, oh no" + (dynamic)"!");
            }
        } while (false);

        dynamic b = x + (dynamic)2020;
        dynamic c = x + (dynamic)231;
        dynamic xa = (dynamic)231342;
        dynamic bx = xa * b + (dynamic)2020;
        dynamic cz = c ^ (dynamic)2000 * bx;
        dynamic z = x + b + c + cz;
        var flag69420 = false;
        do {
            var attached = typeof(Debugger).GetProperty("IsAttached");
            dynamic A = (dynamic)1 + (dynamic)124;
            var getmeth = attached.GetGetMethod();
            dynamic B = (dynamic)1 + (dynamic)23 - 1232;
            var returna = (bool)getmeth.Invoke(null,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty, null, null,
                CultureInfo.CurrentCulture);
            dynamic C = A + B;

            var log = typeof(Debugger).GetMethod("IsLogging");

            var returnb = (bool)log.Invoke((dynamic)1 + (dynamic)1 == (dynamic)2 ? null : null,
                BindingFlags.Static | BindingFlags.Public, null,
                null,
                CultureInfo.InvariantCulture);

            flag69420 = returna || returnb;
            dynamic baba = x + xa;
            if ((flag69420 as dynamic) == (dynamic)true)
                baba *= Math.Sqrt(bx);

            if (flag69420 && baba == x + xa * Math.Sqrt(bx)) {
                dynamic texasda = "Exception " + (dynamic)"🦅";
                if (true || true)
                    Environment.FailFast(texasda + (dynamic)" Is " + (dynamic)" Null : " + (dynamic)"🦅");
                dynamic adfas = -2179238;
                adfas = Math.Abs(-adfas);
                adfas = +1;
                Environment.FailFast(adfas);
            }

            Detect();
            Thread.Sleep(100);
            z = (dynamic)1 + c + cz;
            returna = (bool)getmeth.Invoke(null,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty, null, null,
                CultureInfo.CurrentCulture);
            c = (dynamic)1 + cz + x;
            returnb = (bool)log.Invoke(null, BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                null,
                null,
                CultureInfo.InvariantCulture);
            z = x + b + c + cz;
            baba = z + b;
            flag69420 = flag69420 || returna || returnb;
            if (!flag69420) {
                dynamic xiuAGSDHUiyas =
                    "grrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrr";
                if (Debugger.IsAttached || Debugger.IsLogging())
                    baba = x + xa * Math.Sqrt(bx);
                dynamic xadsad =
                    "grrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrrgrrrrrr";
                xadsad = xiuAGSDHUiyas + xadsad + 6 * 2;
                flag69420 = xadsad == xiuAGSDHUiyas;
                returna = (bool)getmeth.Invoke(null,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty, null, null,
                    CultureInfo.CurrentCulture);
                returnb = (bool)log.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, null,
                    CultureInfo.InvariantCulture);
            }
        } while (x + b + c + cz == z);
    }
}
