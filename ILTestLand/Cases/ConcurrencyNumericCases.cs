using System.Numerics;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1700, BogusStates = 6, OpaqueDepth = 3, DecryptHashChance = 0.65)]
internal static class ConcurrencyNumericCases
{
    public static bool Case1001() { var task = Task.Run(() => 1001); return task.Result == 1001; }
    public static bool Case1002() { var tasks = Enumerable.Range(1, 4).Select(value => Task.Run(() => value * value)).ToArray(); Task.WaitAll(tasks); return tasks.Select(task => task.Result).Sum() == 30; }
    public static bool Case1003() { using var source = new CancellationTokenSource(); source.Cancel(); return source.IsCancellationRequested; }
    public static bool Case1004() { using var gate = new ManualResetEventSlim(); var task = Task.Run(() => { gate.Set(); return 1004; }); gate.Wait(); return task.Result == 1004; }
    public static bool Case1005() { var value = 0; Parallel.For(0, 5, index => Interlocked.Add(ref value, index)); return value == 10; }
    public static bool Case1006() { var value = 0; Parallel.ForEach(new[] { 1, 2, 3, 1000 }, item => Interlocked.Add(ref value, item)); return value == 1006; }
    public static bool Case1007() { var bag = new System.Collections.Concurrent.ConcurrentBag<int>(); Parallel.For(0, 7, bag.Add); return bag.Count == 7 && bag.Sum() == 21; }
    public static bool Case1008() { var map = new System.Collections.Concurrent.ConcurrentDictionary<int, int>(); map.AddOrUpdate(8, 8, (_, old) => old + 1); map.AddOrUpdate(8, 8, (_, old) => old + 1); return map[8] == 9; }
    public static bool Case1009() { var lazy = new Lazy<int>(() => 1009); return lazy.Value == 1009 && lazy.IsValueCreated; }
    public static bool Case1010() { var barrier = new Barrier(2); var left = Task.Run(() => { barrier.SignalAndWait(); return 10; }); var right = Task.Run(() => { barrier.SignalAndWait(); return 1000; }); return Task.WaitAll(new[] { left, right }, 2000) && left.Result + right.Result == 1010; }
    public static bool Case1011() { var values = Enumerable.Range(1, 11).ToArray(); return values.AsParallel().Sum() == 66; }
    public static bool Case1012() { var source = System.Threading.Channels.Channel.CreateUnbounded<int>(); _ = Task.Run(async () => await source.Writer.WriteAsync(1012)); return source.Reader.ReadAsync().AsTask().Result == 1012; }
    public static bool Case1013() { var completed = Task.WhenAll(Task.FromResult(1000), Task.FromResult(13)).Result; return completed.Sum() == 1013; }
    public static bool Case1014() { var value = Task.FromResult(14).ContinueWith(task => task.Result + 1000).Result; return value == 1014; }
    public static bool Case1015() { var timeout = Task.Delay(1).Wait(2000); return timeout; }
    public static bool Case1016() { var token = new CancellationTokenSource().Token; return Task.FromCanceled<int>(new CancellationToken(true)).IsCanceled; }
    public static bool Case1017() { var values = new[] { 1000, 17 }; var result = values.Aggregate(0, (sum, value) => sum + value); return result == 1017; }
    public static bool Case1018() { var value = BigInteger.Pow(new BigInteger(2), 10) + 18; return value == 1042; }
    public static bool Case1019() { var value = BigInteger.GreatestCommonDivisor(new BigInteger(1019), new BigInteger(19)); return value == 1; }
    public static bool Case1020() { var value = new Complex(3, 4); return value.Magnitude == 5; }
    public static bool Case1021() { var value = new Complex(10, 21); return value.Real == 10 && value.Imaginary == 21; }
    public static bool Case1022() { var value = new BigInteger(1022); return value.ToString() == "1022"; }
    public static bool Case1023() { var value = BitOperations.RotateLeft(1u, 10); return value == 1024; }
    public static bool Case1024() { var value = BitOperations.PopCount(0b1111_1111u); return value == 8; }
    public static bool Case1025() { var value = BitOperations.TrailingZeroCount(1024u); return value == 10; }
    public static bool Case1026() { var value = Math.Clamp(2000, 0, 1026); return value == 1026; }
    public static bool Case1027() { var value = Math.FusedMultiplyAdd(3, 9, 1000); return value == 1027; }
    public static bool Case1028() { var value = Math.ScaleB(1.0, 10) + 4; return value == 1028; }
    public static bool Case1029() { var value = Math.Log2(1024) + 5; return value == 15; }
    public static bool Case1030() { var value = Math.DivRem(2030, 2, out var remainder); return value == 1015 && remainder == 0; }
    public static bool Case1031() { var value = Enumerable.Range(1, 31).Where(number => number % 2 != 0).Sum(); return value == 256; }
    public static bool Case1032() { var value = Enumerable.Range(1, 32).Aggregate(BigInteger.One, (product, number) => product * number); return value == BigInteger.Parse("263130836933693530167218012160000000"); }
    public static bool Case1033() { var value = Enumerable.Range(1, 33).Select(number => number * number).Sum(); return value == 12529; }
    public static bool Case1034() { var value = Enumerable.Range(1, 34).Select(number => number.ToString()).Aggregate(string.Concat); return value.Length == 59; }
    public static bool Case1035() { var values = new[] { 10, 20, 30, 35 }; return values.Zip(values.Skip(1)).All(pair => pair.Second > pair.First); }
    public static bool Case1036() { var values = Enumerable.Repeat(1036, 3).ToArray(); return values.SequenceEqual(new[] { 1036, 1036, 1036 }); }
    public static bool Case1037() { var values = Enumerable.Range(1, 37).Chunk(10).Select(chunk => chunk.Sum()).ToArray(); return values.Length == 4 && values[^1] == 238; }
    public static bool Case1038() { var value = Enumerable.Range(1, 38).SelectMany(number => new[] { number, -number }).Sum(); return value == 0; }
    public static bool Case1039() { var value = Enumerable.Range(1, 39).GroupBy(number => number % 3).ToDictionary(group => group.Key, group => group.Count()); return value[0] == 13; }
    public static bool Case1040() { var value = Enumerable.Range(1, 40).Aggregate(new HashSet<int>(), (set, number) => { set.Add(number % 7); return set; }); return value.Count == 7; }
    public static bool Case1041() { Span<int> values = stackalloc int[] { 1000, 41 }; return values[0] + values[1] == 1041; }
    public static bool Case1042() { var bytes = new byte[2]; System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes, 1042); return System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes) == 1042; }
    public static bool Case1043() { var bytes = new byte[4]; System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes, 1043); return System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes) == 1043; }
    public static bool Case1044() { var value = new decimal(1044, 0, 0, false, 2); return value == 10.44m; }
    public static bool Case1045() { var value = TimeSpan.FromMinutes(17).Add(TimeSpan.FromSeconds(25)); return value.TotalSeconds == 1045; }
    public static bool Case1046() { var value = DateOnly.FromDateTime(new DateTime(2026, 9, 8)).DayOfYear; return value == 251; }
    public static bool Case1047() { var value = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(1047)); return value.Second == 27; }
    public static bool Case1048() { var value = new Guid("00000000-0000-0000-0000-000000001048"); return value.ToString("N").EndsWith("1048", StringComparison.Ordinal); }
    [DoNotVirtualize]
    public static bool Case1049() { return NonVirtualConstant(1049) == 1049; }
    public static bool Case1050() { ObfuscationMacros.VMBegin(); var value = 1000 + 50; ObfuscationMacros.VMEnd(); return value == 1050; }

    private static int NonVirtualConstant(int value) => value;
}
