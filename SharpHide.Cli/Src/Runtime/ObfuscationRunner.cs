using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using ILVerify;
using Spectre.Console;
using SharpHide.Core;
using SharpHide.Core.Engine;
using AssemblyNameInfo = System.Reflection.Metadata.AssemblyNameInfo;
using IResolver = ILVerify.IResolver;

namespace SharpHide.Cli;

internal static class ObfuscationRunner {
    public static void Banner() {
        AnsiConsole.Write(new FigletText("SharpHide").Color(Color.Purple));
        AnsiConsole.Write(new Rule("[grey]extensible .NET obfuscator[/]").LeftJustified().RuleStyle("grey"));
    }

    public static int Run(SharpHideProject project, bool verify, bool run) {
        var input = project.Input;
        var output = project.ResolveOutput();

        AnsiConsole.MarkupLineInterpolated($"[cyan]loading[/] {input}");
        var module = ModuleDefMD.Load(input);

        var random = project.Seed is { } seed ? new Random(seed)
            : int.TryParse(Environment.GetEnvironmentVariable("SHARP_SEED"), out var envSeed) ? new Random(envSeed)
            : new Random();

        try {
            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .Start(progress => {
                    var tasks = new Dictionary<string, ProgressTask>(StringComparer.Ordinal);
                    new Obfuscator {
                        Settings = project.ToSettings(),
                        DisabledProtections = project.DisabledProtections(),
                        Options = project.OptionOverrides(),
                    }.Obfuscate(new ObfuscationContext(module, random) {
                        Log = WriteLog,
                        Progress = update => UpdateProgress(progress, tasks, update),
                    });
                });
        }
        catch (Exception error) when (IsUserStringHeapOverflow(error)) {
            AnsiConsole.MarkupLine("[red]obfuscation stopped: metadata user-string heap is too large (#US heap).[/]");
            AnsiConsole.MarkupLine("[yellow]Reduce generated string count: disable opaque-predicate and junk-method,[/]");
            AnsiConsole.MarkupLine("lower control-flow max-size/bogus-states/handlers, or reduce string-encryption junk-chance.[/]");
            AnsiConsole.MarkupLine("[grey]No output was written. Adjust the .shproj options and retry.[/]");
            return 1;
        }

        CopySidecar(input, output, ".runtimeconfig.json");
        CopySidecar(input, output, ".deps.json");
        AnsiConsole.MarkupLineInterpolated($"[green]done[/] -> {output}");

        if (verify && VerifyIL(input, output) is var verifyCode and not 0)
            return verifyCode;

        if (!run)
            return 0;
        return RunIsolated(input, output);
    }

    private static void UpdateProgress(ProgressContext progress, Dictionary<string, ProgressTask> tasks,
        ProtectionProgress update) {
        if (!tasks.TryGetValue(update.Name, out var task)) {
            task = progress.AddTask(update.Name, maxValue: 1);
            tasks[update.Name] = task;
        }

        var phase = $"{update.Stage} {update.Position}/{update.Total}";
        task.Description = update.State switch {
            ProtectionProgressState.Started => $"{update.Name} ({phase}, running)",
            ProtectionProgressState.Completed => $"{update.Name} ({phase}, done)",
            ProtectionProgressState.Failed => $"{update.Name} ({phase}, failed: {update.Detail})",
            _ => update.Name,
        };
        task.Value = update.State == ProtectionProgressState.Completed ? 1 : 0;
    }

    private static bool IsUserStringHeapOverflow(Exception error) {
        for (var current = error; current is not null; current = current.InnerException)
            if (current.Message.Contains("#US heap is too big", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // Compare verifier error categories before and after obfuscation.
    private static int VerifyIL(string input, string output) {
        try {
            var directories = ReferenceDirectories().ToList();
            var baseline = VerifyAssembly(input, directories);
            var obfuscated = VerifyAssembly(output, directories);

            var regressions = obfuscated
                .Where(category => !UnsafeButLegal.Contains(category.Key)
                                   && category.Value > baseline.GetValueOrDefault(category.Key))
                .OrderByDescending(category => category.Value)
                .ToList();

            AnsiConsole.MarkupLineInterpolated(
                $"[cyan]ilverify[/] input={baseline.Values.Sum()} output={obfuscated.Values.Sum()} errors");
            if (regressions.Count > 0) {
                foreach (var category in regressions)
                    AnsiConsole.MarkupLineInterpolated(
                        $"[red]  +{category.Value - baseline.GetValueOrDefault(category.Key)} {category.Key}[/]");
                AnsiConsole.MarkupLine("[red]IL verification regressed: obfuscation broke IL[/]");
                return 1;
            }
            AnsiConsole.MarkupLine("[green]IL verification OK[/] (no new error categories beyond the input baseline)");
            return 0;
        }
        catch (Exception error) {
            AnsiConsole.MarkupLineInterpolated($"[yellow]IL verification skipped: {error.Message}[/]");
            return 0;
        }
    }

    private static readonly HashSet<VerifierError> UnsafeButLegal = new() {
        VerifierError.None, VerifierError.Unverifiable, VerifierError.ReturnPtrToStack, VerifierError.InitOnly,
        VerifierError.ExpectedNumericType, VerifierError.StackUnexpected,
    };

    private static Dictionary<VerifierError, int> VerifyAssembly(string path, List<string> referenceDirectories) {
        var directories = new List<string>(referenceDirectories) { Path.GetDirectoryName(Path.GetFullPath(path))! };
        var verifier = new Verifier(new FrameworkResolver(directories), new VerifierOptions { SanityChecks = true });
        verifier.SetSystemModuleName(new AssemblyNameInfo("System.Private.CoreLib"));

        using var stream = File.OpenRead(path);
        using var peReader = new PEReader(stream);
        var counts = new Dictionary<VerifierError, int>();
        foreach (var result in verifier.Verify(peReader))
            counts[result.Code] = counts.GetValueOrDefault(result.Code) + 1;
        return counts;
    }

    private static IEnumerable<string> ReferenceDirectories() {
        var core = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory().TrimEnd('\\', '/');
        yield return core;
        var desktop = core.Replace("Microsoft.NETCore.App", "Microsoft.WindowsDesktop.App");
        if (desktop != core && Directory.Exists(desktop))
            yield return desktop;
    }

    private sealed class FrameworkResolver(IEnumerable<string> directories) : IResolver {
        private readonly string[] directories = directories.ToArray();
        private readonly Dictionary<string, PEReader?> cache = new(StringComparer.OrdinalIgnoreCase);

        public PEReader? ResolveAssembly(AssemblyNameInfo assemblyName) => Resolve(assemblyName.Name ?? string.Empty);

        public PEReader? ResolveModule(AssemblyNameInfo referencingAssembly, string fileName) =>
            Resolve(Path.GetFileNameWithoutExtension(fileName));

        private PEReader? Resolve(string simpleName) {
            if (cache.TryGetValue(simpleName, out var cached))
                return cached;
            PEReader? reader = null;
            foreach (var directory in directories) {
                var path = Path.Combine(directory, simpleName + ".dll");
                if (!File.Exists(path))
                    continue;
                reader = new PEReader(File.OpenRead(path));
                break;
            }
            cache[simpleName] = reader;
            return reader;
        }
    }

    // Run output alone under its original assembly name.
    private static int RunIsolated(string input, string output) {
        var directory = Path.Combine(Path.GetTempPath(), "sharphide-run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var assemblyFile = Path.GetFileName(input);
        File.Copy(output, Path.Combine(directory, assemblyFile), overwrite: true);
        CopyInto(input, directory, ".runtimeconfig.json");
        CopyInto(input, directory, ".deps.json");

        var target = Path.Combine(directory, assemblyFile);
        AnsiConsole.MarkupLineInterpolated($"[cyan]running (isolated)[/] {target}");
        var process = Process.Start(new ProcessStartInfo("dotnet", $"\"{target}\"") {
            UseShellExecute = false,
            WorkingDirectory = directory,
        })!;
        process.WaitForExit();
        return process.ExitCode;
    }

    private static void CopyInto(string input, string directory, string suffix) {
        var source = Path.ChangeExtension(input, null) + suffix;
        if (File.Exists(source))
            File.Copy(source, Path.Combine(directory, Path.GetFileNameWithoutExtension(input) + suffix), overwrite: true);
    }

    private static void WriteLog(string message) {
        var escaped = Markup.Escape(message);
        AnsiConsole.MarkupLine(message.StartsWith("[*]") ? $"[bold cyan]{escaped}[/]" : $"[grey]{escaped}[/]");
    }

    private static void CopySidecar(string input, string output, string suffix) {
        var source = Path.ChangeExtension(input, null) + suffix;
        if (File.Exists(source))
            File.Copy(source, Path.ChangeExtension(output, null) + suffix, overwrite: true);
    }
}
