# ILTestLand

Run from the repository root:

```powershell
dotnet run --project ILTestLand/ILTestLand.csproj -c Release
dotnet run --project SharpHide.Cli/SharpHide.Cli.csproj -c Release -- run ILTestLand/virtualization.shproj --run
```

The first command runs the native baseline. The second virtualizes 2119 methods and retains unsupported selective regions, combines pooling, numeric/string encoding, resource encryption, renaming, and CFF, then runs an isolated copy. Both executions must return zero and finish with `ALL PASS (3200 checks)`.

Case implementations live under `Cases/`; `Harness/Program.cs` is the runner and `virtualization.shproj` remains the release profile.

VM checks cover branching loops, generic reference returns under compacting GC, exception-filter ordering, normal and exceptional finally paths, native stack allocation, 64-bit arithmetic, class and method markers, explicit opt-out, and selective regions. `SharpHide.Vendor` supplies compile-time markers without becoming a protected-runtime dependency.

Stress coverage adds 14 deterministic families across 200 inputs: virtualized arithmetic, branches, 64-bit values, arrays, strings, exceptions, generics, nullable values, method selection, accepted and rejected regions, dictionaries, decimals, and LINQ sequences. Passing families report one summary line; failures retain exact family and input names.

`virtualization.shproj` is a reproducible heavy profile, not a preset claiming resistance to deobfuscation. It uses three VM instances, per-method opcode and byte-stream scrambling, 64 XTEA rounds, authenticated payloads with per-method/instance-bound keys, a 1 MiB payload bound, a project-level VM key derivation salt, four-layer numeric encoding, and keyed strict anti-tamper checks with diagnostics disabled. Ref-like arguments, locals, value returns, and managed-reference returns are supported. The VM key changes build-derived keys; it is not a runtime secret once embedded.

The fixture selects both string decryptors for virtualization (`virtualize-chance: 1`) to exercise that integration deterministically. Ordinary projects default to `0`; selected handlers incur VM execution cost on each call. Interpreter strings use a separate non-virtualized decoder.
