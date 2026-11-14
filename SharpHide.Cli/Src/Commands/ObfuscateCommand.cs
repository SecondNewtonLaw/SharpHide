using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using SharpHide.Core;

namespace SharpHide.Cli;

public sealed class ObfuscateCommand : Command<ObfuscateCommand.Settings> {
    public sealed class Settings : CommandSettings {
        [CommandArgument(0, "<assembly>")]
        [Description("Path to the .NET assembly to obfuscate.")]
        public string Input { get; init; } = string.Empty;

        [CommandArgument(1, "[output]")]
        [Description("Output path (default: Obfuscated-<input> next to the input).")]
        public string? Output { get; init; }

        [CommandOption("--run")]
        [Description("Run the obfuscated assembly with dotnet after writing it.")]
        public bool Run { get; init; }

        [CommandOption("--keep-names")]
        [Description("Keep injected member names readable (e.g. a debug build shipping a custom PDB).")]
        public bool KeepNames { get; init; }

        [CommandOption("--vm-key <KEY>")]
        [Description("Derive per-method VM encryption and authentication keys from KEY when creating a project.")]
        public string? VmKey { get; init; }

        [CommandOption("--reflect-rate <RATE>")]
        [Description("Fraction of eligible call/delegate sites routed through reflective dispatch.")]
        [DefaultValue(0.5)]
        public double ReflectRate { get; init; }

        [CommandOption("--junk-rate <RATE>")]
        [Description("Fraction of valid user methods that get dead crash-junk prepended.")]
        [DefaultValue(0.3)]
        public double JunkRate { get; init; }

        [CommandOption("--verify")]
        [Description("Run ilverify on the output and fail if obfuscation introduced IL errors beyond the input.")]
        public bool Verify { get; init; }

        public override ValidationResult Validate() =>
            File.Exists(Input) ? ValidationResult.Success() : ValidationResult.Error($"assembly not found: {Input}");
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation) {
        ObfuscationRunner.Banner();

        var projectPath = Path.ChangeExtension(settings.Input, ".shproj");
        SharpHideProject project;
        if (File.Exists(projectPath)) {
            project = SharpHideProject.Load(projectPath);
            AnsiConsole.MarkupLineInterpolated(
                $"[grey]using project[/] {projectPath} [grey](edit it to tune; CLI rate flags ignored)[/]");
        }
        else {
            project = SharpHideProject.CreateDefault(settings.Input);
            project.Output = settings.Output;
            project.KeepNames = settings.KeepNames;
            project.VirtualizationKey = settings.VmKey;
            var reflectRate = Math.Clamp(settings.ReflectRate, 0, 1);
            project.Protections["call-hiding"].Options["rate"] = reflectRate;
            project.Protections["delegate-hiding"].Options["rate"] = reflectRate;
            project.Protections["junk-method"].Options["rate"] = Math.Clamp(settings.JunkRate, 0, 1);
            try {
                project.Save(projectPath);
                AnsiConsole.MarkupLineInterpolated(
                    $"[yellow]created project[/] {projectPath} [grey]tune payloads there, then `SharpHide run {Path.GetFileName(projectPath)}`[/]");
            }
            catch (Exception error) {
                AnsiConsole.MarkupLineInterpolated(
                    $"[yellow]could not write project {projectPath}: {error.Message} (continuing with defaults)[/]");
            }
        }
        return ObfuscationRunner.Run(project, settings.Verify, settings.Run);
    }
}
