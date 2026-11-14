using System.Globalization;
using SharpHide.Core;
using Spectre.Console;

namespace SharpHide.Cli;

internal static class InteractiveTerminal {
    private static readonly string[] MainChoices = [
        "1. Create project",
        "2. Load project",
        "3. Edit project settings",
        "4. Save project",
        "5. Obfuscate",
        "6. Obfuscate and verify",
        "7. Obfuscate and run isolated",
        "8. Exit",
    ];

    public static int Run() {
        var session = new Session();
        while (true) {
            Render(session);
            try {
                switch (PromptChoice("Main menu", MainChoices)) {
                    case "1. Create project":
                        CreateProject(session);
                        break;
                    case "2. Load project":
                        LoadProject(session);
                        break;
                    case "3. Edit project settings":
                        EditProject(session);
                        break;
                    case "4. Save project":
                        SaveProject(session);
                        break;
                    case "5. Obfuscate":
                        Obfuscate(session, verify: false, run: false);
                        break;
                    case "6. Obfuscate and verify":
                        Obfuscate(session, verify: true, run: false);
                        break;
                    case "7. Obfuscate and run isolated":
                        Obfuscate(session, verify: false, run: true);
                        break;
                    case "8. Exit":
                        return 0;
                }
            }
            catch (Exception error) {
                AnsiConsole.MarkupLineInterpolated($"[red]operation failed:[/] {Markup.Escape(error.Message)}");
                Pause();
            }
        }
    }

    private static void Render(Session session) {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("SharpHide").Color(Color.Purple));
        AnsiConsole.Write(new Rule("[grey]interactive terminal[/]").LeftJustified().RuleStyle("grey"));
        if (session.Project is null) {
            AnsiConsole.MarkupLine("[yellow]No project loaded.[/] Create or load one to begin.");
            return;
        }

        var project = session.Project;
        AnsiConsole.MarkupLineInterpolated($"[cyan]input[/] {Markup.Escape(project.Input)}");
        AnsiConsole.MarkupLineInterpolated($"[cyan]output[/] {Markup.Escape(project.ResolveOutput())}");
        if (session.ProjectPath is not null)
            AnsiConsole.MarkupLineInterpolated($"[cyan]project[/] {Markup.Escape(session.ProjectPath)}");

        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("Protection")
            .AddColumn("State")
            .AddColumn("Options");
        foreach (var entry in project.Protections.OrderBy(entry => entry.Key, StringComparer.Ordinal)) {
            var options = string.Join(", ", entry.Value.Options.Select(option =>
                $"{option.Key}={option.Value.ToString("R", CultureInfo.InvariantCulture)}"));
            table.AddRow(Markup.Escape(entry.Key), entry.Value.Enabled ? "[green]on[/]" : "[grey]off[/]",
                Markup.Escape(options));
        }
        AnsiConsole.Write(table);
    }

    private static void CreateProject(Session session) {
        var input = PromptPath("Assembly path", mustExist: true);
        var project = SharpHideProject.CreateDefault(input);
        ConfigureGeneral(project);
        var path = PromptPath("Project path", mustExist: false, Path.ChangeExtension(input, ".shproj"));
        if (File.Exists(path) && !AnsiConsole.Confirm($"Overwrite {path}?", defaultValue: false))
            return;
        project.Save(path);
        session.Project = project;
        session.ProjectPath = path;
        AnsiConsole.MarkupLineInterpolated($"[green]created[/] {Markup.Escape(path)}");
        Pause();
    }

    private static void LoadProject(Session session) {
        var path = PromptPath("Project path", mustExist: true);
        session.Project = SharpHideProject.Load(path);
        session.ProjectPath = path;
        AnsiConsole.MarkupLineInterpolated($"[green]loaded[/] {Markup.Escape(path)}");
        Pause();
    }

    private static void EditProject(Session session) {
        if (session.Project is null) {
            AnsiConsole.MarkupLine("[yellow]Create or load project first.[/]");
            Pause();
            return;
        }

        while (true) {
            var choice = PromptChoice("Project editor", [
                "1. General settings", "2. Toggle protection", "3. Edit numeric option", "4. Save", "5. Back",
            ]);
            switch (choice) {
                case "1. General settings":
                    ConfigureGeneral(session.Project);
                    break;
                case "2. Toggle protection":
                    ToggleProtection(session.Project);
                    break;
                case "3. Edit numeric option":
                    EditOption(session.Project);
                    break;
                case "4. Save":
                    SaveProject(session);
                    break;
                case "5. Back":
                    return;
            }
        }
    }

    private static void ConfigureGeneral(SharpHideProject project) {
        var output = PromptOptional("Output path", project.Output ?? project.ResolveOutput());
        project.Output = string.IsNullOrWhiteSpace(output) ? null : output;
        project.KeepNames = AnsiConsole.Confirm("Keep injected names?", project.KeepNames);

        var seed = PromptOptional("Seed (blank for random)", project.Seed?.ToString(CultureInfo.InvariantCulture));
        if (string.IsNullOrWhiteSpace(seed))
            project.Seed = null;
        else if (int.TryParse(seed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSeed))
            project.Seed = parsedSeed;
        else
            AnsiConsole.MarkupLine("[yellow]Seed unchanged: enter a signed 32-bit integer.[/]");

        var keyPrompt = new TextPrompt<string>("VM key (blank keeps current)").AllowEmpty().Secret();
        var key = AnsiConsole.Prompt(keyPrompt);
        if (!string.IsNullOrWhiteSpace(key))
            project.VirtualizationKey = key;
    }

    private static void ToggleProtection(SharpHideProject project) {
        if (project.Protections.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]Project contains no protection entries.[/]");
            Pause();
            return;
        }
        var choices = project.Protections.OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key} [{(entry.Value.Enabled ? "on" : "off")}]").ToArray();
        var selected = PromptChoice("Protection", choices);
        var name = selected[..selected.IndexOf(" [", StringComparison.Ordinal)];
        project.Protections[name].Enabled = !project.Protections[name].Enabled;
    }

    private static void EditOption(SharpHideProject project) {
        var protections = project.Protections.Where(entry => entry.Value.Options.Count > 0)
            .OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray();
        if (protections.Length == 0) {
            AnsiConsole.MarkupLine("[yellow]No numeric options available.[/]");
            Pause();
            return;
        }

        var protectionChoice = PromptChoice("Protection", protections.Select(entry => entry.Key).ToArray());
        var config = project.Protections[protectionChoice];
        var optionChoice = PromptChoice("Option", config.Options.OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key} = {entry.Value.ToString("R", CultureInfo.InvariantCulture)}").ToArray());
        var optionName = optionChoice[..optionChoice.IndexOf(" =", StringComparison.Ordinal)];
        if (config.Help?.TryGetValue(optionName, out var help) == true)
            AnsiConsole.MarkupLineInterpolated($"[grey]{Markup.Escape(help)}[/]");
        config.Options[optionName] = PromptNumber("New value", config.Options[optionName]);
    }

    private static void SaveProject(Session session) {
        if (session.Project is null) {
            AnsiConsole.MarkupLine("[yellow]Create or load project first.[/]");
            Pause();
            return;
        }
        var path = PromptPath("Project path", mustExist: false, session.ProjectPath ?? Path.ChangeExtension(session.Project.Input, ".shproj"));
        if (File.Exists(path) && !AnsiConsole.Confirm($"Overwrite {path}?", defaultValue: true))
            return;
        session.Project.Save(path);
        session.ProjectPath = path;
        AnsiConsole.MarkupLineInterpolated($"[green]saved[/] {Markup.Escape(path)}");
        Pause();
    }

    private static void Obfuscate(Session session, bool verify, bool run) {
        if (session.Project is null) {
            AnsiConsole.MarkupLine("[yellow]Create or load project first.[/]");
            Pause();
            return;
        }
        var result = ObfuscationRunner.Run(session.Project, verify, run);
        AnsiConsole.MarkupLine(result == 0 ? "[green]operation completed[/]" : $"[red]operation exited {result}[/]");
        Pause();
    }

    private static string PromptChoice(string title, IEnumerable<string> choices) =>
        AnsiConsole.Prompt(new SelectionPrompt<string>().Title(title).PageSize(14).AddChoices(choices));

    private static string PromptPath(string title, bool mustExist, string? defaultValue = null) {
        var prompt = new TextPrompt<string>(title).PromptStyle("green");
        if (defaultValue is not null)
            prompt.DefaultValue(defaultValue);
        if (mustExist)
            prompt.Validate(path => File.Exists(path)
                ? ValidationResult.Success()
                : ValidationResult.Error($"file not found: {path}"));
        return AnsiConsole.Prompt(prompt);
    }

    private static string PromptOptional(string title, string? defaultValue) {
        var prompt = new TextPrompt<string>(title).AllowEmpty();
        if (defaultValue is not null)
            prompt.DefaultValue(defaultValue);
        return AnsiConsole.Prompt(prompt);
    }

    private static double PromptNumber(string title, double current) {
        var prompt = new TextPrompt<string>(title)
            .DefaultValue(current.ToString("R", CultureInfo.InvariantCulture))
            .Validate(value => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                && double.IsFinite(parsed)
                ? ValidationResult.Success()
                : ValidationResult.Error("enter a finite number"));
        return double.Parse(AnsiConsole.Prompt(prompt), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static void Pause() {
        AnsiConsole.MarkupLine("[grey]Press Enter to continue.[/]");
        Console.ReadLine();
    }

    private sealed class Session {
        public SharpHideProject? Project { get; set; }
        public string? ProjectPath { get; set; }
    }
}
