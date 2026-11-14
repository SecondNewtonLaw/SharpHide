using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using SharpHide.Core;
using SharpHide.Core.Engine;

namespace SharpHide.Cli;

public sealed class NewProjectCommand : Command<NewProjectCommand.Settings> {
    public sealed class Settings : CommandSettings {
        [CommandArgument(0, "<assembly>")]
        [Description("Assembly the project will obfuscate.")]
        public string Input { get; init; } = string.Empty;

        [CommandArgument(1, "[project]")]
        [Description("Project file to write (default: <assembly>.shproj).")]
        public string? Project { get; init; }

        [CommandOption("--force")]
        [Description("Overwrite an existing project file.")]
        public bool Force { get; init; }

        public override ValidationResult Validate() =>
            File.Exists(Input) ? ValidationResult.Success() : ValidationResult.Error($"assembly not found: {Input}");
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation) {
        ObfuscationRunner.Banner();
        var path = settings.Project ?? Path.ChangeExtension(settings.Input, ".shproj");
        if (File.Exists(path) && !settings.Force) {
            AnsiConsole.MarkupLineInterpolated($"[red]project exists:[/] {path} [grey](use --force to overwrite)[/]");
            return 1;
        }

        SharpHideProject.CreateDefault(settings.Input).Save(path);
        AnsiConsole.MarkupLineInterpolated($"[green]created[/] {path}");
        foreach (var protection in Protection.Discover().OrderByDescending(protection => protection.Priority))
            AnsiConsole.MarkupLineInterpolated($"  [green]on[/]  {protection.Name} [grey]{protection.Description}[/]");
        return 0;
    }
}
