# Virtualization support matrix

| Family | Current path | Unsupported behavior |
| --- | --- | --- |
| Arithmetic, bitwise, comparisons | Precompiled VM handlers | Compiler rejects invalid stack/type shapes |
| Calls, fields, boxing, casts, conversions | Compiler-emitted static bridges | Compiler rejects unsupported signatures |
| Arrays and memory operations | Compiler-emitted static bridges | Compiler rejects unsupported element/signature types |
| `volatile.`, `readonly.`, `unaligned.` | Prefix flags carried into static bridges | Compiler rejects duplicate or invalid prefix placement |
| Managed references and ref returns | Compiler accessors plus native-cell banks | Compiler rejects unsafe lifetime shapes |
| Exceptions, filters, finally, fault | Compiler-emitted host/unwind IL | Compiler rejects invalid region edges |
| `localloc` | VM allocation state plus host `localloc` outside handlers | Compiler rejects handler-local allocation |
| Generics | Generic host/bridge/accessor clones with recursive signature substitution | Compiler rejects unresolved runtime metadata |
| `tail.` | Lowered call semantics | Compiler rejects malformed prefix placement |
| `jmp` | Compiler lowers compatible jumps to call plus return | Signature, generic context, and exception-region checks reject invalid transfers |
| `arglist` / varargs | Native-cell bridge path | Compiler rejects unsupported signatures |
| Function pointers and indirect calls | Static bridge with `IntPtr`/signature metadata | Compiler rejects unsupported call-site signatures |
| Native integer arithmetic | Precompiled VM arithmetic handlers and pointer bridges | Compiler rejects unsupported overflow/type combinations |
| `ref TypedReference` | Typed-reference native-cell path | Compiler rejects unsupported return shapes |
| `ref struct` | Native-cell path when layout is representable | Compiler rejects unsafe storage or lifetime |
| Selective VM regions | Region helper when boundaries are valid | Permissive mode retains original region; strict mode rejects method |

Authenticated payloads use versioned `SHB1` envelopes. Algorithm 1 uses HMAC-SHA-256 over header plus encrypted bytes; algorithm 2 uses AES-GCM with a per-payload nonce and 128-bit tag. Optional project `VirtualizationKey` material derives per-method keys at build time; embedded binaries remain self-contained. Legacy `SHAT` envelopes remain readable. Production malformed-bytecode failures use silent fail-fast. Debug mode enables opaque VM integrity exceptions.

Compiler rejects VM payloads above configured `payload-max-size` (default 1 MiB, hard maximum 16 MiB); envelope overhead is 46 bytes for HMAC and 42 bytes for AES-GCM. Runtime rejects oversized encoded or decoded payloads before interpretation.

Bridge or host emission failures are transactional: original IL remains installed, generated members are removed, and newly created VM runtime types are rolled back.

## Remaining limitations

- `tail.` lowers to an ordinary static bridge call; CLR tail-frame elimination is not preserved.
- `jmp` is accepted only for exact compatible signatures outside exception regions and lowers to call plus return.
- Unsupported signatures and invalid selective-region boundaries retain original IL in permissive mode; strict region mode rejects the method.
- Payload and anti-tamper keys are build-derived and embedded in the protected assembly. Authentication detects mutation but does not provide a runtime-held secret or confidentiality against a determined binary analyst.
- `VmNativeCode` remains an isolated x86 prototype and is not part of production VM execution.
- Direct unscoped `VmReference` helpers retain process-local default scope for standalone runner compatibility; generated protected stubs pass per-frame scopes.
