using System.Collections.Concurrent;
using System.Diagnostics;

internal static class AsyncSyncCases
{
    public static bool Case2401() { return Task.FromResult(2401).GetAwaiter().GetResult() == 2401; }
    public static bool Case2402() { return Task.Run(() => 2402).GetAwaiter().GetResult() == 2402; }
    public static bool Case2403() { return Task.WhenAll(Task.FromResult(2), Task.FromResult(3), Task.FromResult(4)).GetAwaiter().GetResult().Sum() == 9; }
    public static bool Case2404() { return Task.WhenAny(Task.FromResult(2404), Task.Delay(1000)).GetAwaiter().GetResult().IsCompleted; }
    public static bool Case2405() { return Task.CompletedTask.IsCompletedSuccessfully; }
    public static bool Case2406() { return Task.FromException<int>(new InvalidOperationException()).IsFaulted; }
    public static bool Case2407() { return Task.FromCanceled<int>(new CancellationToken(true)).IsCanceled; }
    public static bool Case2408() { using var source = new CancellationTokenSource(); source.Cancel(); return source.IsCancellationRequested; }
    public static bool Case2409() { using var source = new CancellationTokenSource(); source.CancelAfter(1); return source.Token.CanBeCanceled; }
    public static bool Case2410() { return new CancellationTokenSource().Token.WaitHandle != null; }
    public static bool Case2411() { var value = new ManualResetEventSlim(); value.Set(); return value.IsSet; }
    public static bool Case2412() { using var value = new SemaphoreSlim(1); return value.Wait(0); }
    public static bool Case2413() { using var value = new Mutex(); return value.WaitOne(0); }
    public static bool Case2414() { using var value = new AutoResetEvent(false); value.Set(); return value.WaitOne(0); }
    public static bool Case2415() { using var value = new CountdownEvent(2); value.Signal(); value.Signal(); return value.IsSet; }
    public static bool Case2416() { using var value = new Barrier(2); return value.ParticipantCount == 2; }
    public static bool Case2417() { var value = new SpinLock(); var taken = false; value.Enter(ref taken); value.Exit(); return taken; }
    public static bool Case2418() { var value = new SpinWait(); value.SpinOnce(); return value.Count == 1; }
    public static bool Case2419() { var value = new object(); lock (value) return true; }
    public static bool Case2420() { var value = 0; Interlocked.Increment(ref value); return value == 1; }
    public static bool Case2421() { var value = 2; return Interlocked.CompareExchange(ref value, 3, 2) == 2 && value == 3; }
    public static bool Case2422() { var value = 5; return Interlocked.Add(ref value, 2) == 7; }
    public static bool Case2423() { var value = 7; return Interlocked.Exchange(ref value, 3) == 7 && value == 3; }
    public static bool Case2424() { var value = 9L; return Interlocked.Read(ref value) == 9; }
    public static bool Case2425() { var value = 0; Thread.MemoryBarrier(); return value == 0; }
    public static bool Case2426() { var value = new ConcurrentQueue<int>(); value.Enqueue(2426); return value.TryDequeue(out var item) && item == 2426; }
    public static bool Case2427() { var value = new ConcurrentStack<int>(); value.Push(2427); return value.TryPop(out var item) && item == 2427; }
    public static bool Case2428() { var value = new ConcurrentBag<int>(); value.Add(2428); return value.Contains(2428); }
    public static bool Case2429() { var value = new ConcurrentDictionary<int, int>(); value[2429] = 1; return value.TryGetValue(2429, out var item) && item == 1; }
    public static bool Case2430() { var value = new BlockingCollection<int>(); value.Add(2430); return value.Take() == 2430; }
    public static bool Case2431() { var value = new ThreadLocal<int>(() => 2431); return value.Value == 2431; }
    public static bool Case2432() { using var value = new AsyncLocalHolder(2432); return value.Value == 2432; }
    public static bool Case2433() { var value = new Lazy<int>(() => 2433); return value.Value == 2433 && value.IsValueCreated; }
    public static bool Case2434() { var value = new Lazy<Task<int>>(() => Task.FromResult(2434)); return value.Value.GetAwaiter().GetResult() == 2434; }
    public static bool Case2435() { var value = new ValueTask<int>(2435); return value.Result == 2435; }
    public static bool Case2436() { var value = new ValueTask( Task.CompletedTask); return value.IsCompletedSuccessfully; }
    public static bool Case2437() { var value = new TaskCompletionSource<int>(); value.SetResult(2437); return value.Task.Result == 2437; }
    public static bool Case2438() { var value = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously); value.TrySetResult(2438); return value.Task.Result == 2438; }
    public static bool Case2439() { return ParallelEnumerable.Range(1, 3).Sum() == 6; }
    public static bool Case2440() { var value = 0; Parallel.For(0, 4, _ => Interlocked.Increment(ref value)); return value == 4; }
    public static bool Case2441() { var value = new[] { 1, 2, 3 }; var sum = 0; Parallel.ForEach(value, item => Interlocked.Add(ref sum, item)); return sum == 6; }
    public static bool Case2442() { return ParallelEnumerable.Range(1, 4).AsOrdered().SequenceEqual(new[] { 1, 2, 3, 4 }.AsParallel()); }
    public static bool Case2443() { var value = new Thread(() => { }); value.Start(); value.Join(); return value.ThreadState != System.Threading.ThreadState.Unstarted; }
    public static bool Case2444() { return Thread.CurrentThread.ManagedThreadId > 0; }
    public static bool Case2445() { return Thread.CurrentThread.IsAlive; }
    public static bool Case2446() { return ThreadPool.QueueUserWorkItem(_ => { }); }
    public static bool Case2447() { using var value = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite); return value != null; }
    public static bool Case2448() { return Stopwatch.GetTimestamp() > 0; }
    public static bool Case2449() { var value = Stopwatch.StartNew(); value.Stop(); return value.Elapsed >= TimeSpan.Zero; }
    public static bool Case2450() { return Environment.ProcessorCount > 0; }

    private sealed class AsyncLocalHolder : IDisposable
    {
        private readonly AsyncLocal<int> value = new();
        public AsyncLocalHolder(int item) { value.Value = item; }
        public int Value => value.Value;
        public void Dispose() { value.Value = 0; }
    }
}
