using Spectre.Console.Cli;

namespace SharpHide.Cli;

internal static class Program {
    private static int Main(string[] args) {
        if (args.Length == 0)
            return InteractiveTerminal.Run();

        var app = new CommandApp<ObfuscateCommand>();
        app.Configure(config => {
            config.SetApplicationName("SharpHide");
            config.AddCommand<ObfuscateCommand>("obfuscate")
                .WithDescription("Obfuscate an assembly and create a .shproj beside it on first run.");
            config.AddCommand<NewProjectCommand>("new")
                .WithDescription("Scaffold a .shproj project file for an assembly.");
            config.AddCommand<RunProjectCommand>("run")
                .WithDescription("Obfuscate an assembly using a .shproj project file.");
        });
        return app.Run(args);
    }
}
