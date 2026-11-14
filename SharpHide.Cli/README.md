# SharpHide CLI

Run `SharpHide` without arguments to open the terminal UI. Use arrow keys and Enter to choose actions:

1. create project from an assembly;
2. load an existing `.shproj`;
3. edit protection switches and numeric options;
4. save the current project;
5. obfuscate;
6. obfuscate and run IL verification;
7. obfuscate and run the output in isolation;
8. exit.

Script-friendly commands remain available:

```text
SharpHide obfuscate <assembly> [output] [options]
SharpHide new <assembly> [project] [--force]
SharpHide run <project> [--verify] [--run]
```
