using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using SharpHide.Core;

namespace SharpHide.Cli;

public sealed class RunProjectCommand : Command<RunProjectCommand.Settings> {
    public sealed class Settings : CommandSettings {
        [CommandArgument(0, "<project>")]
        [Description("Path to a .shproj project file.")]
        public string Project { get; init; } = string.Empty;

        [CommandOption("--run")]
        [Description("Run the obfuscated assembly with dotnet after writing it.")]
        public bool Run { get; init; }

        [CommandOption("--verify")]
        [Description("Run ilverify on the output and fail if obfuscation introduced IL errors beyond the input.")]
        public bool Verify { get; init; }

        public override ValidationResult Validate() =>
            File.Exists(Project) ? ValidationResult.Success() : ValidationResult.Error($"project not found: {Project}");
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation) {
        ObfuscationRunner.Banner();
        var project = SharpHideProject.Load(settings.Project);
        if (!File.Exists(project.Input)) {
            AnsiConsole.MarkupLineInterpolated($"[red]input assembly not found:[/] {project.Input}");
            return 1;
        }
        return ObfuscationRunner.Run(project, settings.Verify, settings.Run);
    }
}
