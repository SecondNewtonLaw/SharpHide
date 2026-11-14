using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class DiagnosticsRuntimeCases
{
    public static bool Case1801() { var value = Stopwatch.GetTimestamp(); return value > 0; }
    public static bool Case1802() { var value = Stopwatch.Frequency; return value > 0; }
    public static bool Case1803() { var value = Stopwatch.StartNew(); value.Stop(); return value.Elapsed >= TimeSpan.Zero; }
    public static bool Case1804() { var value = Stopwatch.StartNew(); value.Restart(); value.Stop(); return value.ElapsedTicks >= 0; }
    public static bool Case1805() { var value = Stopwatch.StartNew(); value.Stop(); value.Reset(); return value.ElapsedTicks == 0; }
    public static bool Case1806() { var value = new Activity("1806"); return value.OperationName == "1806"; }
    public static bool Case1807() { using var value = new Activity("1807"); value.Start(); var result = value.Id != null; value.Stop(); return result; }
    public static bool Case1808() { var value = new ActivitySource("1808"); return value.Name == "1808"; }
    public static bool Case1809() { var value = new ActivityListener { ShouldListenTo = _ => true, Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData }; return value.ShouldListenTo != null; }
    public static bool Case1810() { var value = new DiagnosticListener("1810"); return value.Name == "1810"; }
    public static bool Case1811() { var value = new EventSource("1811"); return value.Name == "1811"; }
    public static bool Case1812() { var value = new EventSourceOptions { Level = EventLevel.Informational, Keywords = EventKeywords.None }; return value.Level == EventLevel.Informational; }
    public static bool Case1813() { using var value = new EventSource("1813"); return value.Name == "1813" && value.Guid != Guid.Empty; }
    public static bool Case1814() { using var value = Process.GetCurrentProcess(); return value.Modules.Count > 0; }
    public static bool Case1815() { var value = new ProcessStartInfo { FileName = "dotnet", Arguments = "--info" }; return value.FileName == "dotnet"; }
    public static bool Case1816() { var value = new ProcessStartInfo("dotnet") { UseShellExecute = false }; return !value.UseShellExecute; }
    public static bool Case1817() { var value = new ProcessStartInfo { RedirectStandardOutput = true }; return value.RedirectStandardOutput; }
    public static bool Case1818() { var value = new ProcessStartInfo { RedirectStandardError = true }; return value.RedirectStandardError; }
    public static bool Case1819() { var value = new ProcessStartInfo { CreateNoWindow = true }; return value.CreateNoWindow; }
    public static bool Case1820() { var value = Process.GetCurrentProcess(); return value.Id > 0; }
    public static bool Case1821() { using var value = Process.GetCurrentProcess(); return value.ProcessName.Length > 0; }
    public static bool Case1822() { var value = Process.GetCurrentProcess().StartTime; return value.Year > 2000; }
    public static bool Case1823() { var value = Environment.TickCount64; return value >= 0; }
    public static bool Case1824() { var value = Environment.TickCount; return value != int.MinValue; }
    public static bool Case1825() { var value = DateTime.UtcNow; return value.Kind == DateTimeKind.Utc; }
    public static bool Case1826() { var value = DateTimeOffset.Now; return value.Offset <= TimeSpan.FromHours(14); }
    public static bool Case1827() { var value = TimeProvider.System.GetUtcNow(); return value.Offset == TimeSpan.Zero; }
    public static bool Case1828() { var value = TimeProvider.System.GetTimestamp(); return value > 0; }
    public static bool Case1829() { var value = TimeProvider.System.GetElapsedTime(0); return value >= TimeSpan.Zero; }
    public static bool Case1830() { var value = new CancellationTokenSource(); return value.Token.WaitHandle.SafeWaitHandle != null; }
    public static bool Case1831() { using var value = new CancellationTokenSource(); return value.Token.CanBeCanceled; }
    public static bool Case1832() { var value = new CancellationToken(false); return !value.IsCancellationRequested; }
    public static bool Case1833() { var value = new CancellationToken(true); return value.IsCancellationRequested; }
    public static bool Case1834() { var value = CancellationToken.None; return !value.CanBeCanceled; }
    public static bool Case1835() { var value = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None); value.Cancel(); return value.IsCancellationRequested; }
    public static bool Case1836() { var value = new Progress<int>(_ => { }); return value != null; }
    public static bool Case1837() { var value = new Progress<int>(); return value != null; }
    public static bool Case1838() { var value = new System.Threading.Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite); value.Dispose(); return true; }
    public static bool Case1839() { using var value = new ManualResetEventSlim(true); return value.IsSet; }
    public static bool Case1840() { using var value = new ManualResetEventSlim(false); value.Set(); value.Reset(); return !value.IsSet; }
    public static bool Case1841() { using var value = new SemaphoreSlim(1, 2); return value.CurrentCount == 1; }
    public static bool Case1842() { using var value = new SemaphoreSlim(0, 1); value.Release(); return value.CurrentCount == 1; }
    public static bool Case1843() { using var value = new CountdownEvent(2); return value.CurrentCount == 2; }
    public static bool Case1844() { using var value = new CountdownEvent(2); value.Signal(2); return value.IsSet; }
    public static bool Case1845() { using var value = new Barrier(2); return value.ParticipantCount == 2; }
    public static bool Case1846() { using var value = new Barrier(1); value.AddParticipant(); return value.ParticipantCount == 2; }
    public static bool Case1847() { using var value = new Barrier(2); value.RemoveParticipant(); return value.ParticipantCount == 1; }
    public static bool Case1848() { var value = new VolatileValue(); Volatile.Write(ref value.Number, 1848); return Volatile.Read(ref value.Number) == 1848; }
    public static bool Case1849() { var value = 0; Interlocked.Exchange(ref value, 1849); return Interlocked.CompareExchange(ref value, 0, 1849) == 1849; }
    public static bool Case1850() { var value = 0L; Interlocked.Add(ref value, 1850); return value == 1850; }

    private sealed class VolatileValue { public int Number; }
}
