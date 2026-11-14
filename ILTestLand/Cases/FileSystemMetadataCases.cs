using System.IO;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class FileSystemMetadataCases
{
    public static bool Case2051() { var value = new DirectoryInfo(Path.GetTempPath()); return value.Exists; }
    public static bool Case2052() { var value = new DirectoryInfo(Path.GetTempPath()); return value.FullName == Path.GetFullPath(Path.GetTempPath()); }
    public static bool Case2053() { var value = new DirectoryInfo(Path.GetTempPath()); return value.Root != null; }
    public static bool Case2054() { var value = new DirectoryInfo(Path.GetTempPath()); return value.Parent != null; }
    public static bool Case2055() { var value = new DirectoryInfo(Path.GetTempPath()); return value.Attributes.HasFlag(FileAttributes.Directory); }
    public static bool Case2056() { var value = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-2056")); return !value.Exists; }
    public static bool Case2057() { var value = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-2057")); return value.DirectoryName == Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar); }
    public static bool Case2058() { var value = new FileInfo("2058.txt"); return value.Extension == ".txt"; }
    public static bool Case2059() { var value = new FileInfo("2059.txt"); return value.Name == "2059.txt"; }
    public static bool Case2060() { var value = new FileInfo("2059.txt"); return value.Directory == null || value.Directory.Exists; }
    public static bool Case2061() { var value = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-2061")); return value.CreationTimeUtc <= DateTime.UtcNow; }
    public static bool Case2062() { var value = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-2062")); return value.LastWriteTimeUtc <= DateTime.UtcNow; }
    public static bool Case2063() { var value = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-2063")); return !value.Exists || value.Length == 0; }
    public static bool Case2064() { var value = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-2064")); return !value.Exists || !value.IsReadOnly; }
    public static bool Case2065() { var value = new FileInfo(Path.Combine(Path.GetTempPath(), "missing-2065")); return value.Attributes.HasFlag(FileAttributes.Normal) || !value.Exists; }
    public static bool Case2066() { var value = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "2066")); return Path.IsPathFullyQualified(value); }
    public static bool Case2067() { var value = Path.GetRelativePath(Path.GetTempPath(), Path.Combine(Path.GetTempPath(), "2067")); return value == "2067"; }
    public static bool Case2068() { var value = Path.GetDirectoryName("a/b/2068.txt"); return value != null && value.EndsWith(Path.Combine("a", "b"), StringComparison.Ordinal); }
    public static bool Case2069() { var value = Path.GetFileName("a/b/2069.txt"); return value == "2069.txt"; }
    public static bool Case2070() { var value = Path.GetFileNameWithoutExtension("2070.txt"); return value == "2070"; }
    public static bool Case2071() { var value = Path.GetExtension("2071.txt"); return value == ".txt"; }
    public static bool Case2072() { var value = Path.GetPathRoot("/tmp/2072"); return !string.IsNullOrEmpty(value); }
    public static bool Case2073() { var value = Path.HasExtension("2073.txt"); return value; }
    public static bool Case2074() { var value = Path.ChangeExtension("2074.txt", ".json"); return value == "2074.json"; }
    public static bool Case2075() { var value = Path.Join("a", "b", "2075"); return value.EndsWith("2075", StringComparison.Ordinal); }
    public static bool Case2076() { var value = Path.Combine("a", "b", "2076"); return value.EndsWith(Path.Combine("b", "2076"), StringComparison.Ordinal); }
    public static bool Case2077() { var value = Path.TrimEndingDirectorySeparator("a/b/"); return value == "a/b"; }
    public static bool Case2078() { var value = Path.DirectorySeparatorChar; return value == '/' || value == '\\'; }
    public static bool Case2079() { var value = Path.AltDirectorySeparatorChar; return value == '/' || value == '\\'; }
    public static bool Case2080() { var value = Path.VolumeSeparatorChar; return value != '\0'; }
    public static bool Case2081() { var value = Path.GetInvalidPathChars(); return value.Length > 0; }
    public static bool Case2082() { var value = Path.GetInvalidFileNameChars(); return value.Length > 0; }
    public static bool Case2083() { var value = Directory.GetLogicalDrives(); return value.Length > 0; }
    public static bool Case2084() { var value = Directory.Exists(Path.GetTempPath()); return value; }
    public static bool Case2085() { var value = Directory.EnumerateDirectories(Path.GetTempPath()); return value != null; }
    public static bool Case2086() { var value = Directory.EnumerateFiles(Path.GetTempPath()); return value != null; }
    public static bool Case2087() { var value = Directory.GetParent(Path.GetTempPath()); return value != null; }
    public static bool Case2088() { var value = Directory.GetCreationTimeUtc(Path.GetTempPath()); return value <= DateTime.UtcNow; }
    public static bool Case2089() { var value = Directory.GetLastWriteTimeUtc(Path.GetTempPath()); return value <= DateTime.UtcNow; }
    public static bool Case2090() { var value = new DirectoryInfo(Path.GetTempPath()).Attributes; return value.HasFlag(FileAttributes.Directory); }
    public static bool Case2091() { var value = new FileSystemWatcher(Path.GetTempPath()); return value.Path == Path.GetFullPath(Path.GetTempPath()); }
    public static bool Case2092() { using var value = new FileSystemWatcher(Path.GetTempPath()) { Filter = "*.txt" }; return value.Filter == "*.txt"; }
    public static bool Case2093() { using var value = new FileSystemWatcher(Path.GetTempPath()) { IncludeSubdirectories = true }; return value.IncludeSubdirectories; }
    public static bool Case2094() { using var value = new FileSystemWatcher(Path.GetTempPath()) { NotifyFilter = NotifyFilters.FileName }; return value.NotifyFilter == NotifyFilters.FileName; }
    public static bool Case2095() { using var value = new FileSystemWatcher(Path.GetTempPath()); value.EnableRaisingEvents = false; return !value.EnableRaisingEvents; }
    public static bool Case2096() { using var value = new FileSystemWatcher(Path.GetTempPath()); value.InternalBufferSize = 8192; return value.InternalBufferSize == 8192; }
    public static bool Case2097() { using var value = new FileSystemWatcher(Path.GetTempPath()); value.SynchronizingObject = null; return value.SynchronizingObject == null; }
    public static bool Case2098() { var value = new DriveInfo(Path.GetPathRoot(Path.GetTempPath())!); return value.Name.Length > 0; }
    public static bool Case2099() { var value = DriveInfo.GetDrives(); return value.Length > 0; }
    public static bool Case2100() { var value = new DriveInfo(Path.GetPathRoot(Path.GetTempPath())!); return value.DriveType != DriveType.Unknown || value.DriveType == DriveType.Unknown; }
}
