using System.Threading.Channels;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class AsyncPipelineCases
{
    public static bool Case1301() { return Task.FromResult(1301).GetAwaiter().GetResult() == 1301; }
    public static bool Case1302() { return Task.Run(() => 1302).GetAwaiter().GetResult() == 1302; }
    public static bool Case1303() { var value = Task.Run(async () => await Task.FromResult(1303)).GetAwaiter().GetResult(); return value == 1303; }
    public static bool Case1304() { var value = Task.WhenAll(Task.FromResult(1300), Task.FromResult(4)).GetAwaiter().GetResult(); return value.Sum() == 1304; }
    public static bool Case1305() { var completed = Task.FromResult(1305); var value = Task.WhenAny(completed, Task.Delay(1000)).GetAwaiter().GetResult(); return value == completed && completed.Result == 1305; }
    public static bool Case1306() { var value = Task.Delay(1).ContinueWith(_ => 1306).GetAwaiter().GetResult(); return value == 1306; }
    public static bool Case1307() { using var source = new CancellationTokenSource(); var value = Task.Run(() => 1307, source.Token).GetAwaiter().GetResult(); return value == 1307; }
    public static bool Case1308() { using var source = new CancellationTokenSource(); source.CancelAfter(1000); return source.Token.CanBeCanceled; }
    public static bool Case1309() { using var source = new CancellationTokenSource(); using var registration = source.Token.Register(() => { }); return registration != default; }
    public static bool Case1310() { using var source = new CancellationTokenSource(); var called = 0; using var registration = source.Token.Register(() => called++); source.Cancel(); return called == 1; }
    public static bool Case1311() { var value = ValueTask.FromResult(1311).GetAwaiter().GetResult(); return value == 1311; }
    public static bool Case1312() { async ValueTask<int> Get() => await Task.FromResult(1312); return Get().GetAwaiter().GetResult() == 1312; }
    public static bool Case1313() { var value = Task.FromResult(13).GetAwaiter().GetResult() * 101; return value == 1313; }
    public static bool Case1314() { var value = Enumerable.Range(1, 4).Select(item => Task.FromResult(item)).Select(task => task.Result).Sum(); return value == 10; }
    public static bool Case1315() { var value = Enumerable.Range(1, 5).Select(item => Task.Run(() => item * item)).Select(task => task.Result).Sum(); return value == 55; }
    public static bool Case1316() { var value = Task.Run(() => Enumerable.Range(1, 16).Sum()).Result; return value == 136; }
    public static bool Case1317() { var value = Task.Run(() => Enumerable.Range(1, 17).Where(number => number % 2 == 1).Sum()).Result; return value == 81; }
    public static bool Case1318() { var value = Task.Run(() => Enumerable.Range(1, 18).Select(number => number * 2).Max()).Result; return value == 36; }
    public static bool Case1319() { var value = Task.Run(() => Enumerable.Range(1, 19).Select(number => number * 2).Min()).Result; return value == 2; }
    public static bool Case1320() { var value = Task.Run(() => Enumerable.Range(1, 20).Count(number => number % 4 == 0)).Result; return value == 5; }
    public static bool Case1321() { var channel = Channel.CreateUnbounded<int>(); channel.Writer.TryWrite(1321); return channel.Reader.TryRead(out var value) && value == 1321; }
    public static bool Case1322() { var channel = Channel.CreateBounded<int>(1); return channel.Writer.TryWrite(1322) && channel.Reader.TryRead(out var value) && value == 1322; }
    public static bool Case1323() { var channel = Channel.CreateUnbounded<int>(); channel.Writer.TryWrite(1); channel.Writer.TryWrite(2); channel.Writer.Complete(); var sum = 0; while (channel.Reader.WaitToReadAsync().GetAwaiter().GetResult()) while (channel.Reader.TryRead(out var value)) sum += value; return sum == 3; }
    public static bool Case1324() { var channel = Channel.CreateUnbounded<int>(); channel.Writer.TryWrite(1324); return channel.Reader.WaitToReadAsync().GetAwaiter().GetResult() && channel.Reader.ReadAsync().GetAwaiter().GetResult() == 1324; }
    public static bool Case1325() { var channel = Channel.CreateUnbounded<int>(); channel.Writer.TryComplete(); return channel.Reader.Completion.IsCompleted; }
    public static bool Case1326() { var channel = Channel.CreateUnbounded<int>(); channel.Writer.TryComplete(); channel.Reader.Completion.GetAwaiter().GetResult(); return channel.Reader.Completion.IsCompleted; }
    public static bool Case1327() { var value = Parallel.For(0, 4, _ => { }); return value.IsCompleted; }
    public static bool Case1328() { var value = Parallel.ForEach(new[] { 1, 2, 3 }, _ => { }); return value.IsCompleted; }
    public static bool Case1329() { var total = 0; Parallel.For(1, 5, number => Interlocked.Add(ref total, number)); return total == 10; }
    public static bool Case1330() { var total = 0; Parallel.ForEach(new[] { 1, 2, 3, 4 }, number => Interlocked.Add(ref total, number)); return total == 10; }
    public static bool Case1331() { var value = new Lazy<Task<int>>(() => Task.FromResult(1331)); return value.Value.Result == 1331; }
    public static bool Case1332() { var value = new Lazy<int>(() => 1332, LazyThreadSafetyMode.ExecutionAndPublication); return value.Value == 1332; }
    public static bool Case1333() { var value = new ThreadLocal<int>(() => 1333); return value.Value == 1333; }
    public static bool Case1334() { using var semaphore = new SemaphoreSlim(1, 1); semaphore.Wait(); var value = semaphore.CurrentCount == 0; semaphore.Release(); return value; }
    public static bool Case1335() { using var mutex = new Mutex(); mutex.WaitOne(); mutex.ReleaseMutex(); return true; }
    public static bool Case1336() { using var gate = new ManualResetEventSlim(false); var task = Task.Run(() => { gate.Set(); return 1336; }); gate.Wait(); return task.Result == 1336; }
    public static bool Case1337() { using var gate = new AutoResetEvent(false); var task = Task.Run(() => { gate.Set(); return 1337; }); gate.WaitOne(); return task.Result == 1337; }
    public static bool Case1338() { using var barrier = new Barrier(1); return barrier.SignalAndWait(1000); }
    public static bool Case1339() { using var countdown = new CountdownEvent(2); countdown.Signal(); countdown.Signal(); return countdown.IsSet; }
    public static bool Case1340() { using var source = new CancellationTokenSource(); var value = source.Token.WaitHandle.WaitOne(0); return !value; }
    public static bool Case1341() { var value = new SpinWait(); value.SpinOnce(); return value.Count == 1; }
    public static bool Case1342() { var value = 0; Parallel.Invoke(() => value += 1, () => Interlocked.Add(ref value, 1)); return value == 2; }
    public static bool Case1343() { var value = Task.Run(() => throw new InvalidOperationException()).ContinueWith(task => task.IsFaulted).Result; return value; }
    public static bool Case1344() { var value = Task.FromCanceled<int>(new CancellationToken(true)).ContinueWith(task => task.IsCanceled).Result; return value; }
    public static bool Case1345() { var value = Task.FromException<int>(new Exception()).ContinueWith(task => task.Exception != null).Result; return value; }
    public static bool Case1346() { var value = Task.CompletedTask.Status; return value == TaskStatus.RanToCompletion; }
    public static bool Case1347() { var value = Task.FromResult(1347).IsCompletedSuccessfully; return value; }
    public static bool Case1348() { var value = Task.FromResult(1348).ConfigureAwait(false).GetAwaiter().GetResult(); return value == 1348; }
    public static bool Case1349() { var value = Task.FromResult(1349).WaitAsync(TimeSpan.FromSeconds(1)).Result; return value == 1349; }
    public static bool Case1350() { var value = Task.Run(() => 1350).WaitAsync(TimeSpan.FromSeconds(1)).Result; return value == 1350; }
}
