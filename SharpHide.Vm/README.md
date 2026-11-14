# SharpHide.Vm

Interpreter injected into protected assemblies. Core owns IL compilation, static CLR bridge generation, and pipeline registration. Protected assemblies do not need `SharpHide.Vm.dll` beside them. Runtime uses no dynamic-code generation or third-party packages.

`VmNativeCode` is an isolated x86 prototype outside production VM execution and test coverage.

## Supported methods

Reference `SharpHide.Vendor` and apply `[Virtualize]` to a method or type. `[DoNotVirtualize]` on a method or type overrides selection. User-defined attributes with these exact simple names remain supported.

For selective virtualization, bracket a region with `ObfuscationMacros.VMBegin()` and `VMEnd()`. Markers are removed and enclosed IL moves into a private virtualized helper. Region boundaries require an empty evaluation stack; branches may stay inside but cannot cross either boundary. Methods with exception regions or byref-local reassignment across a selected boundary keep their original IL. Rejected Vendor regions keep CLR IL and lose their no-op calls.

- Static and instance methods, including generic methods, value-type receivers, and instance constructors.
- Integer widths, native integers, floating-point values, references, structs, nullable values, and void returns.
- Arithmetic, checked/unsigned operations, conversions, comparisons, branches, switches, loops, and local/argument storage.
- Calls, virtual and constrained calls, delegate creation, managed/unmanaged indirect calls, fields, arrays, casts, boxing, and metadata handles.
- Managed addresses of arguments, locals, fields, array elements, and boxed values; indirect loads/stores and object initialization/copying.
- Incoming `ref`/`out` arguments, including aliases. Generated stubs pin incoming storage until the interpreter returns.
- Calls returning managed references and `ref readonly` values preserve aliases, including fresh interior references and indirect calls. Native frames retain typed-reference cells and pins through filters, unwinding, and typed returns.
- Pointer and function-pointer values, fixed storage, block initialization/copying, and `constrained.`, `readonly.`, `volatile.`, and `unaligned.` prefixes. Native indirect-call signatures retain optional return modifiers, including calling conventions and `SuppressGCTransition`.
- Catch, filter, finally, fault, leave, throw, and rethrow. Handler search runs in CLR exception filters so enclosing filters execute before unwinding across native and virtualized calls.
- `localloc` uses the host's native stack frame. Repeated allocations remain valid until the invocation returns, including accesses from finally handlers.
- Ref-like arguments, locals, and value returns use typed CLR storage, including `Span<T>`, copies, and calls taking or returning these values by reference. Value returns are copied into caller-owned storage before the callee frame exits; managed-reference returns preserve aliases.
- `mkrefany`, `refanyval`, and `refanytype` operate on evaluation-stack and local typed references, preserving copies, aliases, GC tracking, and CLR type-mismatch exceptions.

Entry points, static constructors, unsupported signatures, and unsupported opcodes retain their original IL. No interpreter is injected when every marked method is skipped.

Bridge or host emission failures roll back generated members and newly created VM runtimes before original IL is retained.

Frames use compiler-calculated stack depth and local/argument counts, each limited to 65,535. Reference-returning calls also require native cells; methods whose native locals exceed the CLR limit retain their original IL. Slots use 32-bit indexes. Invocation state is separate; assigning a value argument does not change the caller's variable. Managed references preserve aliasing and write directly to their referenced storage. Native pointer conversions retain pins through result conversion.

Primitive arithmetic and bitwise operations use precompiled runtime handlers. Other CLR operations use compiler-emitted static bridge methods; runtime dispatches bridge delegates without decoding CLR opcodes. Interpretation still allocates and adds dispatch overhead. Benchmark representative workloads before virtualizing hot loops.

Reference-returning bridges write into caller-owned `TypedReference` locals. Other ref-like values use separate typed native locals for each frame slot. Compiler-emitted accessors copy values and aliases between cells without dynamic accessors. Host refreshes pinned roots only for changed cells. Native local counts include every required storage type and stay within CLR limits.

## Adding an opcode

For an ordinary CLR instruction:

1. Add its `Code` entry to `VmBridgeEmitter.IsSupported`.
2. Check `VmBridgeEmitter.ExactResult`. Compiler derives operand count and exact result type from CLR stack metadata and signatures.
3. Add a marked method to the matching `SharpHide.Tests/Samples/Vm*Samples.cs` and compare the emitted assembly with its CLR baseline in `SharpHide.Tests/VirtualMachine/VirtualizationTests.cs`.

Ordinary instructions use `VmOpcodes.Operation`; compiler emits their CLR instruction into a static bridge. They need no runtime decoder branch or handler class, and CLR exceptions retain normal behavior.

Bitwise and primitive arithmetic operations use randomized VM opcodes with compiler-assigned registers and precompiled `VmBitwise`/`VmArithmetic` handlers. Arithmetic covers 32-bit, 64-bit, native integers, floats, doubles, signed/unsigned division, and checked overflow. Managed-reference arithmetic uses compiler-emitted bridges. Reference accessors and exception hosts are static IL emitted by compiler.

For a precompiled instruction, add its VM opcode, compiler translation, and behavior in the corresponding handler family. Generic math shares arithmetic rules across numeric widths. Add execution and bytecode-shape coverage to the existing primitive-instruction tests.

Prefixes are validated and folded into static bridges by `VmBridgeEmitter`. Instructions changing VM instruction pointer, exception state, or frame lifetime belong in `VmFrame.Step`; their bytecode layout belongs in `VmOpcodes`, compiler emission in `VirtualizationProtection.Emit`, and boundary validation in `VmRunner.CreateFrame`. `localloc` is a lifetime operation requiring CLR host frame.

Varargs bodies capture `arglist` as a typed native cell owned by the generated stub. Vararg call sites preserve fixed and optional parameter types in compiler-emitted bridges.

Valid `tail.` calls and exact-signature `jmp` instructions lower to static call bridges. Values, `starg` updates, return values, and exceptions are preserved; CLR tail-frame elimination is not requested by the lowered bridge.

Value storage belongs in `VmValue`, addresses in `VmReference`, and non-boxable values in `VmNativeValue`/`VmFrame`. Extend these shared paths when adding a type; do not add opcode-specific copies of storage or GC handling.

Run `dotnet test SharpHide.Tests/SharpHide.Tests.csproj --no-restore -c Release --filter "FullyQualifiedName~Virtualization|FullyQualifiedName~VmRunner|FullyQualifiedName~VmFrame"`.

## Program format

All multi-byte values are little-endian. Branch offsets are relative to the bytecode start. Operands are not opcode-mapped.

| Offset | Contents |
| --- | --- |
| 0 | 256-byte map from encoded opcodes to canonical opcodes |
| 256 | Format tag `SVM6` |
| 260 | Temporary-register capacity, signed 32-bit |
| 264 | Local count, signed 32-bit |
| 268 | Argument count, signed 32-bit |
| 272 | Exception-region count, signed 32-bit |
| 276 | Exception table, 28 bytes per region, followed by instruction stream |

Complete program receives fixed scrambling or a per-method keyed byte stream, optional per-method XTEA-CTR encryption, then Base64 encoding. `virtualization.bytecode-rounds` defaults to 32 and accepts integer values 0-64; invalid values reject configuration. Encryption adds decode CPU proportional to rounds and bytecode size but adds zero bytes. This remains reversible obfuscation, not confidentiality: keys and opcode map ship with program. Randomization does not prevent reusable devirtualizer.

`virtualization.instances` selects up to 1-16 isolated injected runtime copies and distributes protected methods across them. It never emits more copies than protected methods. Each extra instance adds roughly one VM payload to output size. `opcode-scrambling` controls per-method opcode-map shuffling. `bytecode-scrambling` selects unique per-method byte-stream keys instead of the legacy fixed scramble. Both scrambling modes add no bytes; byte-stream scrambling adds one linear decode pass.

`payload-auth=1` adds authenticated versioned envelopes. `payload-algorithm=1` uses HMAC-SHA-256; `payload-algorithm=2` uses AES-GCM with a fresh 96-bit nonce and 128-bit tag. Both bind encrypted bytes, method binding, and derived per-method/instance keys. `payload-max-size` defaults to 1 MiB and rejects oversized methods to preserve bounded output and runtime cost.

`number-encryption.layers` composes 1-8 independently keyed MBA decoders around each protected integer. Three to five layers provide the heavy setting; every extra layer adds one call per constant. `handlers` accepts 1-32 and `max-size` accepts 1-100000 to bound expansion.

`constants-pool.rounds` applies 0-64 XTEA-CTR rounds with per-build keys after compression and fixed scrambling. Default 32. It adds one linear startup decode pass and no output bytes.

Anti-tamper trailers use HMAC-SHA-256 with a build-specific embedded key. `anti-tamper.strict=1` rejects unreadable, in-memory, or single-file locations. `anti-tamper.diagnostics=1` exposes an opaque managed integrity exception for debugging; default `0` remains silent fail-fast. `method-checks` inserts 0-64 distributed calls (default `2`) to one initialized verifier; each selected method reads and hashes the assembly on every call, so keep this low outside cold paths.

When constants pooling is enabled, program strings share a compressed resource with other pooled literals. Resource names are randomized per build; the lookup name uses a rolled string decoder independently of the string-encryption setting. Runtime-owned resources bypass generic resource encryption by object identity.

Each exception entry contains seven 32-bit values: try start/end, handler start/end, filter start or `-1`, clause kind (`0` catch, `1` filter, `2` finally, `4` fault), and catch-type metadata index or `-1`. Offsets refer to the instruction stream; end offsets are exclusive.

Each instruction starts with its mapped opcode and a 32-bit register index. The compiler assigns input and destination registers from validated IL dataflow. Execution has no evaluation-stack pointer. Loads write their indicated register; stores, conditions, and returns read it. Binary operations read that register and its successor, then overwrite the first. Duplication copies the indicated register into its successor. Exception entries place their exception value in register zero.

Operation instructions append two 32-bit values: `VmOperation` metadata index and input count. `VmOperation` holds exact result type and compiler-emitted bridge delegate. Conditional comparisons compile as an operation followed by `BrTrue`, preserving unordered floating-point comparisons. Switch appends case count and target offsets; out-of-range indexes fall through.

Bitwise and arithmetic instructions need no operands beyond the common register index. `BitNot` and `Negate` read only their destination register.

Stub supplies an object array containing return type, argument types, local types, operation descriptors, and catch types. Bridge methods carry type, method, field, call-site, string, and numeric operands as ordinary IL metadata, so renaming and metadata rewriting preserve identity.

Compilation validates stack depths across branches and exception entries, including underflow, return depths, and operation prefixes. Runtime checks malformed headers, handler descriptors, operation metadata, boundaries, register spans, and storage bounds. Neither is a full CLR type verifier or sandbox for untrusted programs. Earlier bytecode versions must be rebuilt with matching compiler/runtime.

`virtualization.runtime-errors` defaults to `0`, terminating corrupted VM state through `Environment.FailFast` without diagnostic text. Set it to `1` only for debugging to receive catchable `BadImageFormatException` failures.

Generated protected stubs create per-frame `VmReference.Scope` registries for managed and native-cell accessors. Direct unscoped helpers retain one compatibility scope for standalone runner calls.

String encryption includes interpreter diagnostics through a non-virtualized decoder. `string-encryption.handlers` accepts 1-32; `junk-chance` and `virtualize-chance` accept 0-1. `virtualize-chance` selects generated decryptors for the VM (default `0`); `virtualize-max-size` accepts 0-100000 and caps eligible handler IL instructions (default `4000`). Selected handlers enter the VM on each call. Interpreter strings use a separate decoder to avoid recursive VM initialization. These settings also work without the marked-method virtualization pass.

## Verification

```powershell
dotnet test SharpHide.Tests/SharpHide.Tests.csproj --filter "FullyQualifiedName~Virtualization|FullyQualifiedName~VmRunner"
dotnet test SharpHide.Tests/SharpHide.Tests.csproj -c Release
dotnet build SharpHide.slnx -c Release
```

Tests execute emitted assemblies and compare CLR results, exceptions, and side effects. Coverage includes integer boundaries, floating-point NaNs, large frames, generic calls, constructor initialization order, managed-reference calls and returns under compacting GC, native calling conventions, stack-allocation lifetime, block-memory bounds, exception-filter order, fault clauses, runtime renaming, pooled program lookup, and malformed input.
