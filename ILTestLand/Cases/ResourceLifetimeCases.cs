using System.Buffers;
using System.Runtime.InteropServices;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class ResourceLifetimeCases
{
    public static bool Case1401() { using var resource = new FlagResource(); resource.Touch(); return resource.Touched && !resource.Disposed; }
    public static bool Case1402() { var resource = new FlagResource(); resource.Dispose(); return resource.Disposed; }
    public static bool Case1403() { var resource = new FlagResource(); using (resource) resource.Touch(); return resource.Disposed; }
    public static bool Case1404() { using var resource = new FlagResource(); resource.Dispose(); resource.Dispose(); return resource.Disposed; }
    public static bool Case1405() { var resource = new AsyncFlagResource(); resource.DisposeAsync().GetAwaiter().GetResult(); return resource.Disposed; }
    public static bool Case1406() { var value = new ArrayBufferWriter<byte>(); value.Write(new byte[] { 14, 6 }.AsSpan()); return value.WrittenSpan.SequenceEqual(new byte[] { 14, 6 }); }
    public static bool Case1407() { var value = new ArrayBufferWriter<byte>(2); value.Write(new byte[] { 14, 7 }.AsSpan()); return value.WrittenCount == 2; }
    public static bool Case1408() { var pool = ArrayPool<int>.Shared; var value = pool.Rent(4); value[0] = 1408; var result = value[0] == 1408; pool.Return(value); return result; }
    public static bool Case1409() { var pool = MemoryPool<byte>.Shared; using var memory = pool.Rent(4); memory.Memory.Span[0] = 14; return memory.Memory.Span[0] == 14; }
    public static bool Case1410() { using var owner = MemoryPool<int>.Shared.Rent(2); owner.Memory.Span[0] = 1410; return owner.Memory.Span[0] == 1410; }
    public static bool Case1411() { var value = new MemoryStream(); value.SetLength(1411); return value.Length == 1411; }
    public static bool Case1412() { using var value = new MemoryStream(); value.Capacity = 1412; return value.Capacity >= 1412; }
    public static bool Case1413() { var value = new byte[8]; value.AsSpan().Fill(13); return value.Select(number => (int)number).Sum() == 104; }
    public static bool Case1414() { var value = new byte[8]; value.AsSpan(2, 4).Fill(14); return value[2] == 14 && value[0] == 0; }
    public static bool Case1415() { var value = new int[4]; value.AsSpan().Clear(); return value.All(number => number == 0); }
    public static bool Case1416() { var bytes = new byte[8]; Span<int> value = MemoryMarshal.Cast<byte, int>(bytes.AsSpan()); value[0] = 1416; return value[0] == 1416; }
    public static bool Case1417() { var array = new int[] { 1417 }; var value = MemoryMarshal.AsBytes(array.AsSpan()); return value.Length == sizeof(int); }
    public static bool Case1418() { var array = new int[1]; var value = MemoryMarshal.CreateSpan(ref array[0], 1); value[0] = 1418; return value[0] == 1418; }
    public static bool Case1419() { var value = Marshal.AllocHGlobal(sizeof(int)); try { Marshal.WriteInt32(value, 1419); return Marshal.ReadInt32(value) == 1419; } finally { Marshal.FreeHGlobal(value); } }
    public static bool Case1420() { var value = Marshal.StringToHGlobalUni("1420"); try { return Marshal.PtrToStringUni(value) == "1420"; } finally { Marshal.FreeHGlobal(value); } }
    public static bool Case1421() { var value = Marshal.SizeOf<int>(); return value == 4; }
    public static bool Case1422() { var value = Marshal.OffsetOf<Pair>(nameof(Pair.Second)); return value == 4; }
    public static bool Case1423() { var value = GC.GetGeneration(new object()); return value >= 0; }
    public static bool Case1424() { var value = GC.GetAllocatedBytesForCurrentThread(); GC.KeepAlive(new object()); return value >= 0; }
    public static bool Case1425() { var value = new WeakReference(new object()); return value.IsAlive; }
    public static bool Case1426() { var target = new object(); var value = new WeakReference<object>(target); return value.TryGetTarget(out var found) && ReferenceEquals(target, found); }
    public static bool Case1427() { var value = GCHandle.Alloc(new object(), GCHandleType.Weak); var result = value.IsAllocated; value.Free(); return result; }
    public static bool Case1428() { var value = GCHandle.Alloc(1428, GCHandleType.Pinned); var result = value.Target is int; value.Free(); return result; }
    public static bool Case1429() { var value = GCHandle.Alloc("1429"); var result = (string)value.Target! == "1429"; value.Free(); return result; }
    public static bool Case1430() { var value = GCHandle.Alloc(new[] { 1, 2 }, GCHandleType.Pinned); var address = value.AddrOfPinnedObject(); value.Free(); return address != IntPtr.Zero; }
    public static bool Case1431() { var value = new SafeHandleResource(); value.Dispose(); return value.IsClosed; }
    public static bool Case1432() { using var value = new SafeHandleResource(); return !value.IsClosed; }
    public static bool Case1433() { var value = new Uri("file:///tmp/1433"); return value.IsFile; }
    public static bool Case1434() { var value = Path.GetTempFileName(); File.Delete(value); return value.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase); }
    public static bool Case1435() { var value = Path.GetRandomFileName(); return value.Length == 12; }
    public static bool Case1436() { var value = Path.GetInvalidFileNameChars(); return value.Length > 0; }
    public static bool Case1437() { var value = Path.GetRelativePath("/a/b", "/a/b/c"); return value == "c"; }
    public static bool Case1438() { var value = Path.TrimEndingDirectorySeparator("/a/b/"); return value == "/a/b"; }
    public static bool Case1439() { var value = Path.Join("a", "b", "1439"); return value.EndsWith("1439", StringComparison.Ordinal); }
    public static bool Case1440() { var value = Path.GetFileNameWithoutExtension("report.1440.txt"); return value == "report.1440"; }
    public static bool Case1441() { var value = Directory.GetCurrentDirectory(); return !string.IsNullOrEmpty(value); }
    public static bool Case1442() { var value = Environment.GetEnvironmentVariable("PATH"); return value is not null; }
    public static bool Case1443() { var value = Environment.ProcessorCount; return value > 0; }
    public static bool Case1444() { var value = Environment.NewLine; return value.Length >= 1; }
    public static bool Case1445() { var value = AppContext.BaseDirectory; return Directory.Exists(value); }
    public static bool Case1446() { var value = typeof(ResourceLifetimeCases).Assembly.Location; return value.EndsWith(".dll", StringComparison.OrdinalIgnoreCase); }
    public static bool Case1447() { var value = typeof(ResourceLifetimeCases).Module.ModuleVersionId; return value != Guid.Empty; }
    public static bool Case1448() { var value = new Version(14, 48, 0); return value.Build == 0; }
    public static bool Case1449() { var value = new UriBuilder("file", "", -1, "/tmp/1449").Uri; return value.IsFile; }
    public static bool Case1450() { var value = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-1450")); return !value.Exists; }

    private sealed class FlagResource : IDisposable
    {
        public bool Touched { get; private set; }
        public bool Disposed { get; private set; }
        public void Touch() => Touched = true;
        public void Dispose() => Disposed = true;
    }

    private sealed class AsyncFlagResource : IAsyncDisposable
    {
        public bool Disposed { get; private set; }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }

    private sealed class SafeHandleResource : SafeHandle
    {
        public SafeHandleResource() : base(IntPtr.Zero, true) { SetHandle(new IntPtr(1)); }
        public override bool IsInvalid => handle == IntPtr.Zero;
        protected override bool ReleaseHandle() { handle = IntPtr.Zero; return true; }
    }

    private struct Pair { public int First; public int Second; }
}
