using System.Security.Cryptography;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpHide.Core.Injection;
using SharpHide.Vm;

namespace SharpHide.Core.Engine.Protections;

public sealed class VirtualizationProtection : Protection {
    private sealed record RuntimeOptions(
        bool RuntimeErrors,
        int BytecodeRounds,
        int Instances,
        bool OpcodeScrambling,
        bool BytecodeScrambling,
        bool StrictRegions,
        bool PayloadAuth,
        int PayloadAlgorithm,
        int MaxPayloadSize) {
        public int PayloadOverhead => PayloadAuth ? PayloadAlgorithm == 2 ? 42 : 46 : 0;
    }

    public override long Priority => 92;
    public override string Name => "virtualization";
    public override string Description => "Compiles marked methods to bytecode for an injected VM interpreter";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["runtime-errors"] = 0,
        ["bytecode-rounds"] = 32,
        ["instances"] = 1,
        ["opcode-scrambling"] = 1,
        ["bytecode-scrambling"] = 1,
        ["regions-strict"] = 0,
        ["payload-auth"] = 0,
        ["payload-algorithm"] = 1,
        ["payload-max-size"] = 1_048_576,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["runtime-errors"] =
            "Emit catchable VM integrity exceptions for debugging. Default 0 terminates corrupted bytecode without diagnostics; set 1 while debugging.",
        ["bytecode-rounds"] =
            "Per-method XTEA-CTR bytecode encryption rounds. Default 32; 0 disables it; valid range 0-64. Invalid values are rejected.",
        ["instances"] =
            "Maximum independent injected VM runtime copies. Default 1; range 1-16. More instances increase output size roughly by VM payload size and split methods across distinct runtimes.",
        ["opcode-scrambling"] =
            "Randomize each method's 256-byte opcode map. Default 1; set 0 for deterministic canonical opcodes.",
        ["bytecode-scrambling"] =
            "Use a unique keyed byte stream before bytecode encryption. Default 1; set 0 for the legacy fixed scramble. Adds one linear decode pass and no output growth.",
        ["regions-strict"] =
            "Reject marked methods when selective VMBegin/VMEnd regions cannot be safely lowered. Default 0 keeps original IL and logs rejection.",
        ["payload-auth"] = "Authenticate encrypted VM payloads. Default 0 preserves legacy payload layout.",
        ["payload-algorithm"] =
            "Authenticated payload algorithm: 1 is versioned HMAC, 2 is AES-GCM with a per-payload nonce. Range 1-2.",
        ["payload-max-size"] =
            "Maximum encrypted VM payload bytes, including authentication overhead. Valid range 1-16777216; oversized methods retain original IL and report a fallback.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var methods = context.Module.GetTypes().SelectMany(type => type.Methods).ToArray();
        var selective = methods.Where(method => !IsExcluded(method) && VmRegionEmitter.HasMarkers(method)).ToArray();
        var settings = ReadOptions(context.Options.GetValueOrDefault(Name));
        var regionMethods = new List<MethodDef>();
        foreach (var method in selective) {
            var helpers = VmRegionEmitter.Create(method);
            if (helpers.Count == 0) {
                context.Log($"    selective region rejected: {method.FullName}");
                if (settings.StrictRegions)
                    throw new InvalidOperationException($"Selective VM region rejected: {method.FullName}");
            }
            else
                regionMethods.AddRange(helpers);
        }

        var regions = regionMethods.ToArray();
        var marked = methods.Where(method => !selective.Contains(method) &&
                                             !IsExcluded(method) && IsMarked(method));
        var count = Virtualize(context, marked.Concat(regions), settings);
        RemoveVendorMacroCalls(methods);
        RemoveMarkerAttributes(context.Module);
        context.Log($"    {count} methods virtualized");
    }

    private static bool IsMarked(MethodDef method) => HasAttribute(method, "VirtualizeAttribute") ||
                                                      HasAttribute(method.DeclaringType, "VirtualizeAttribute");

    private static bool IsExcluded(MethodDef method) => HasAttribute(method, "DoNotVirtualizeAttribute") ||
                                                        HasAttribute(method.DeclaringType, "DoNotVirtualizeAttribute");

    private static bool HasAttribute(IHasCustomAttribute owner, string name) =>
        owner.CustomAttributes.Any(attribute => attribute.AttributeType.Name == name);

    private static void RemoveMarkerAttributes(ModuleDef module) {
        foreach (var type in module.GetTypes()) {
            RemoveMarkerAttributes(type);
            foreach (var method in type.Methods)
                RemoveMarkerAttributes(method);
        }
    }

    private static void RemoveMarkerAttributes(IHasCustomAttribute owner) {
        foreach (var attribute in owner.CustomAttributes.Where(attribute =>
                     attribute.AttributeType.Name == "VirtualizeAttribute" ||
                     attribute.AttributeType.Name == "DoNotVirtualizeAttribute").ToArray())
            owner.CustomAttributes.Remove(attribute);
    }

    private static void RemoveVendorMacroCalls(IEnumerable<MethodDef> methods) {
        foreach (var method in methods.Where(method => method.HasBody)) {
            var changed = false;
            foreach (var instruction in method.Body.Instructions.Where(instruction =>
                         instruction.OpCode.Code is Code.Call or Code.Callvirt &&
                         instruction.Operand is IMethod target &&
                         target.DeclaringType.FullName == "SharpHide.Vendor.ObfuscationMacros" &&
                         (target.Name == "VMBegin" || target.Name == "VMEnd"))) {
                instruction.OpCode = OpCodes.Nop;
                instruction.Operand = null;
                changed = true;
            }

            if (changed) {
                method.Body.OptimizeMacros();
                method.Body.OptimizeBranches();
            }
        }
    }

    public static int Virtualize(ObfuscationContext context, IEnumerable<MethodDef> methods) {
        return Virtualize(context, methods, ReadOptions(context.Options.GetValueOrDefault("virtualization")));
    }

    private static int Virtualize(ObfuscationContext context, IEnumerable<MethodDef> methods, RuntimeOptions settings) {
        var module = context.Module;
        context.Log(
            $"    settings: instances={settings.Instances}, rounds={settings.BytecodeRounds}, opcode-scrambling={(settings.OpcodeScrambling ? 1 : 0)}, bytecode-scrambling={(settings.BytecodeScrambling ? 1 : 0)}, payload-auth={(settings.PayloadAuth ? 1 : 0)}, payload-algorithm={(settings.PayloadAuth ? settings.PayloadAlgorithm : 0)}, payload-max-size={settings.MaxPayloadSize}");
        var runtimes = new InjectResult?[settings.Instances];
        var fallbackReasons = new Dictionary<string, int>(StringComparer.Ordinal);
        var maxCells = 0;
        var maxNativeBanks = 0;
        var maxUnwindDepth = 0;
        var maxHostSlots = 0;
        var count = 0;
        foreach (var method in methods.Distinct().ToArray()) {
            string? fallbackReason = null;
            if (!IsEligible(method) || context.IsStartupCritical(method) ||
                !TryCompile(method, context.Random, out var bytecode, out var metadata, out var nativeTypes,
                    out var accessTypes, out var cellCount, out var unwindDepth, out fallbackReason,
                    settings.OpcodeScrambling)) {
                if (fallbackReason is not null)
                    fallbackReasons[fallbackReason] = fallbackReasons.GetValueOrDefault(fallbackReason) + 1;
                continue;
            }

            if (bytecode.Length + settings.PayloadOverhead > settings.MaxPayloadSize) {
                fallbackReasons["payload exceeds configured max size"] =
                    fallbackReasons.GetValueOrDefault("payload exceeds configured max size") + 1;
                continue;
            }

            var originalBody = method.Body;
            var originalMethods = method.DeclaringType.Methods.ToHashSet();
            var originalTypes = module.GetTypes().ToHashSet();
            var runtimeIndex = count % settings.Instances;
            var runtime = runtimes[runtimeIndex];
            var runtimeCreated = runtime is null;
            if (runtime is null) {
                runtime = module.Inject(typeof(VmRunner), true);
                runtime.Include(typeof(VmTransfer));
                foreach (var type in module.GetTypes().Where(t => !originalTypes.Contains(t))) {
                    context.Runtime.Add(type);
                    context.VirtualMachineTypes.Add(type);
                }

                runtimes[runtimeIndex] = runtime;
            }

            MethodDef host;
            VmReferenceAccessDescriptor[] accessors;
            try {
                foreach (var descriptor in metadata.OfType<VmBridgeDescriptor>())
                    descriptor.Method = VmBridgeEmitter.Create(module, method, runtime, descriptor);
                accessors = accessTypes.Select(type => VmReferenceAccessEmitter.Create(module, method, runtime, type,
                    VmTypeLayout.Bank(method, type, nativeTypes) >= 0)).ToArray();
                host = VmHostEmitter.Create(module, method, runtime, nativeTypes, cellCount, unwindDepth);
                long scrambleKey;
                do {
                    scrambleKey = settings.BytecodeScrambling ? context.Random.NextInt64() : 0;
                } while (settings.BytecodeScrambling && scrambleKey == 0);

                if (settings.BytecodeScrambling)
                    bytecode = VmRunner.Scramble(VmRunner.Scramble(bytecode), scrambleKey);
                var key0 = settings.BytecodeRounds == 0 ? 0 : context.Random.NextInt64();
                var key1 = settings.BytecodeRounds == 0 ? 0 : context.Random.NextInt64();
                long bindingKey;
                do {
                    bindingKey = context.Random.NextInt64();
                } while (bindingKey == 0);

                (key0, key1) = DerivePayloadKeys(context.Settings.VirtualizationKey, key0, key1,
                    method.FullName, bindingKey, runtimeIndex);
                bytecode = VmRunner.Encrypt(bytecode, key0, key1, settings.BytecodeRounds);
                var payload = settings.PayloadAuth
                    ? settings.PayloadAlgorithm == 2
                        ? VmRunner.SealAesVersioned(bytecode, key0, key1, bindingKey)
                        : VmRunner.SealVersioned(bytecode, key0, key1, bindingKey)
                    : Convert.ToBase64String(bytecode);
                ReplaceBody(module, method, runtime, host, payload, metadata, nativeTypes, accessors,
                    settings.RuntimeErrors, scrambleKey, key0, key1, settings.BytecodeRounds, bindingKey);
            }
            catch (Exception error) when (error is NotSupportedException or ArgumentException
                                              or InvalidOperationException or TypeLoadException
                                              or BadImageFormatException) {
                method.Body = originalBody;
                foreach (var generated in method.DeclaringType.Methods
                             .Where(candidate => !originalMethods.Contains(candidate)).ToArray())
                    method.DeclaringType.Methods.Remove(generated);
                if (runtimeCreated) {
                    RollbackRuntime(module, context, originalTypes);
                    runtimes[runtimeIndex] = null;
                }

                var reason = error.Message.Length == 0 ? "compiler bridge emission failed" : error.Message;
                fallbackReasons[reason] = fallbackReasons.GetValueOrDefault(reason) + 1;
                continue;
            }

            maxCells = Math.Max(maxCells, cellCount);
            maxNativeBanks = Math.Max(maxNativeBanks, nativeTypes.Count);
            maxUnwindDepth = Math.Max(maxUnwindDepth, unwindDepth);
            maxHostSlots = Math.Max(maxHostSlots, method.Parameters.Count + method.Body.Variables.Count);
            count++;
        }

        if (count != 0)
            context.Log($"    {runtimes.Count(runtime => runtime is not null)} VM instances emitted");
        if (count != 0)
            context.Log(
                $"    host preflight: max-cells={maxCells}, max-native-banks={maxNativeBanks}, max-unwind={maxUnwindDepth}, max-slots={maxHostSlots}");
        if (fallbackReasons.Count != 0)
            context.Log(
                $"    fallback reasons: {string.Join("; ", fallbackReasons.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key} ({pair.Value})"))}");
        return count;
    }

    private static RuntimeOptions ReadOptions(IReadOnlyDictionary<string, double>? options) {
        return new RuntimeOptions(
            FlagOption(options, "runtime-errors", 0),
            IntegerOption(options, "bytecode-rounds", 0, 0, 64),
            IntegerOption(options, "instances", 1, 1, 16),
            FlagOption(options, "opcode-scrambling", 1),
            FlagOption(options, "bytecode-scrambling", 0),
            FlagOption(options, "regions-strict", 0),
            FlagOption(options, "payload-auth", 0),
            IntegerOption(options, "payload-algorithm", 1, 1, 2),
            IntegerOption(options, "payload-max-size", 1_048_576, 1, 16_777_216));
    }

    private static bool FlagOption(IReadOnlyDictionary<string, double>? options, string name, double fallback) {
        var value = options?.GetValueOrDefault(name) ?? fallback;
        if (value is not (0 or 1))
            throw new ArgumentOutOfRangeException(name, value, "Expected binary value 0 or 1.");
        return value > 0;
    }

    private static int IntegerOption(IReadOnlyDictionary<string, double>? options, string name, double fallback,
        int minimum, int maximum) {
        var value = options?.GetValueOrDefault(name) ?? fallback;
        if (value < minimum || value > maximum || Math.Abs(value - Math.Truncate(value)) > float.Epsilon)
            throw new ArgumentOutOfRangeException(name, value, $"Expected integer in range {minimum}..{maximum}.");
        return (int)value;
    }

    private static void RollbackRuntime(ModuleDef module, ObfuscationContext context,
        IReadOnlySet<TypeDef> originalTypes) {
        foreach (var type in module.Types.Where(type => !originalTypes.Contains(type)).ToArray())
            module.Types.Remove(type);
        foreach (var type in context.Runtime.Where(type => !originalTypes.Contains(type)).ToArray())
            context.Runtime.Remove(type);
        foreach (var type in context.VirtualMachineTypes.Where(type => !originalTypes.Contains(type)).ToArray())
            context.VirtualMachineTypes.Remove(type);
    }

    private static (long Key0, long Key1) DerivePayloadKeys(string? userKey, long key0, long key1,
        string methodIdentity, long binding, int instance) {
        if (string.IsNullOrEmpty(userKey))
            return (key0, key1);
        var secret = SHA256.HashData(Encoding.UTF8.GetBytes(userKey));
        var identity = Encoding.UTF8.GetBytes(methodIdentity);
        var material = new byte[28 + identity.Length];
        BitConverter.TryWriteBytes(material, key0);
        BitConverter.TryWriteBytes(material.AsSpan(8), key1);
        BitConverter.TryWriteBytes(material.AsSpan(16), binding);
        BitConverter.TryWriteBytes(material.AsSpan(24), instance);
        identity.CopyTo(material.AsSpan(28));
        var derived = HMACSHA256.HashData(secret, material);
        return (BitConverter.ToInt64(derived), BitConverter.ToInt64(derived, 8));
    }

    private static bool IsByRefLike(TypeSig signature) {
        var type = signature.RemovePinnedAndModifiers();
        if (type is GenericInstSig instance)
            type = instance.GenericType;

        if (type is not ClassOrValueTypeSig named) return type.ElementType == ElementType.TypedByRef;

        var definition = named.TypeDefOrRef.ResolveTypeDef();

        if (definition?.CustomAttributes.Any(a =>
                a.TypeFullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute") == true)
            return true;

        return (Type.GetType(named.AssemblyQualifiedName, false) ??
                typeof(object).Assembly.GetType(named.FullName))?.IsByRefLike == true;
    }

    private static bool SupportsType(TypeSig signature, bool allowRefLike = false) {
        var type = signature.RemovePinnedAndModifiers();
        if (type is ClassOrValueTypeSig)
            return allowRefLike || !IsByRefLike(type);
        return type.ElementType switch {
            ElementType.TypedByRef => allowRefLike,
            ElementType.Ptr or ElementType.ByRef or ElementType.Array or ElementType.SZArray => SupportsType(type.Next,
                allowRefLike),
            ElementType.FnPtr => SupportsType(((FnPtrSig)type).MethodSig.RetType, allowRefLike) &&
                                 ((FnPtrSig)type).MethodSig.Params.All(parameterType =>
                                     SupportsType(parameterType, allowRefLike)) &&
                                 ((FnPtrSig)type).MethodSig.ParamsAfterSentinel?.All(parameterType =>
                                     SupportsType(parameterType, allowRefLike)) != false,
            ElementType.GenericInst => ((GenericInstSig)type).GenericArguments.All(argumentType =>
                                           SupportsType(argumentType, allowRefLike)) &&
                                       SupportsType(((GenericInstSig)type).GenericType, allowRefLike),
            _ => true,
        };
    }

    private static bool IsEligible(MethodDef method) {
        if (!method.HasBody || method.MethodSig is not { } signature)
            return false;
        var returnType = signature.RetType.RemovePinnedAndModifiers();
        var argumentCount = signature.Params.Count + (signature.HasThis ? 1 : 0) +
                            (method.Body.Instructions.Any(instruction => instruction.OpCode.Code == Code.Arglist)
                                ? 1
                                : 0);
        return SupportsType(returnType, true) &&
               argumentCount <= VmOpcodes.MaxFrameSize &&
               signature.Params.All(parameterType => SupportsType(parameterType, true)) &&
               signature.ParamsAfterSentinel?.All(parameterType => SupportsType(parameterType, true)) != false &&
               method.Body.Variables.Count <= VmOpcodes.MaxFrameSize &&
               method.Body.Variables.All(local => SupportsType(local.Type, true));
    }

    private static bool TryCompile(MethodDef method, Random rng, out byte[] bytecode, out List<object> metadata,
        out List<TypeSig> nativeTypes, out List<TypeSig> accessTypes, out int cellCount, out int unwindDepth,
        out string? fallbackReason, bool opcodeScrambling) {
        bytecode = [];
        metadata = [method.MethodSig.RetType.ToTypeDefOrRef()];
        nativeTypes = [];
        accessTypes = [];
        cellCount = -1;
        unwindDepth = method.Body.ExceptionHandlers.Count(handler => handler.IsFinally || handler.IsFault);
        fallbackReason = "unsupported or invalid IL";
        var hasArglist = method.Body.Instructions.Any(instruction => instruction.OpCode.Code == Code.Arglist);
        method.Parameters.UpdateParameterTypes();
        metadata.AddRange(method.Parameters.Select(p => (object)p.Type.ToTypeDefOrRef()));
        if (hasArglist)
            metadata.Add(method.Module.CorLibTypes.GetTypeRef("System", nameof(RuntimeArgumentHandle)));
        metadata.AddRange(method.Body.Variables.Select(v => (object)v.Type.ToTypeDefOrRef()));
        if (method.Body.Instructions.Where((t, index) => t.OpCode.Code == Code.Tailcall &&
                                                         (index > method.Body.Instructions.Count - 3 ||
                                                          method.Body.Instructions[index + 1].OpCode.Code is not (Code
                                                              .Call or Code.Callvirt or Code.Calli) ||
                                                          method.Body.Instructions[index + 2].OpCode.Code != Code.Ret))
            .Any()) {
            return false;
        }

        var instructions = method.Body.Instructions;
        var positions = instructions.Select((instruction, index) => (instruction, index))
            .ToDictionary(entry => entry.instruction, entry => entry.index);
        var heights = Enumerable.Repeat(-1, instructions.Count).ToArray();
        var pending = new Queue<int>();
        var validStack = instructions.Count != 0;
        var maxStack = 0;

        if (validStack)
            Merge(instructions[0], 0);
        foreach (var handler in method.Body.ExceptionHandlers) {
            Merge(handler.TryStart, 0);
            Merge(handler.HandlerStart, handler.IsCatch || handler.IsFilter ? 1 : 0);
            if (handler.IsFilter)
                Merge(handler.FilterStart, 1);
        }

        while (validStack && pending.TryDequeue(out var position)) {
            var instruction = instructions[position];
            instruction.CalculateStackUsage(out var pushes, out var pops);
            var code = instruction.OpCode.Code;
            pops = code switch {
                Code.Ret => method.MethodSig.RetType.ElementType == ElementType.Void ? 0 : 1,
                Code.Endfinally or Code.Rethrow => 0,
                Code.Leave or Code.Leave_S => heights[position],
                _ => pops
            };
            if (pops < 0 || heights[position] < pops ||
                code is Code.Ret or Code.Endfinally or Code.Endfilter && heights[position] != pops ||
                code == Code.Jmp && heights[position] != 0 ||
                code == Code.Localloc && heights[position] != 1)
                return false;
            var after = heights[position] - pops + pushes;
            switch (instruction.Operand) {
                case Instruction target:
                    Merge(target, after);
                    break;
                case IList<Instruction> targets: {
                    foreach (var branchTarget in targets)
                        Merge(branchTarget, after);
                    break;
                }
            }

            if (code != Code.Jmp &&
                instruction.OpCode.FlowControl is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw))
                Merge(position + 1 < instructions.Count ? instructions[position + 1] : null, after);
        }

        if (!validStack)
            return false;
        var registerCount = Math.Max(1, maxStack);
        if (method.Body.Instructions.Any(instruction => instruction.OpCode.Code == Code.Jmp))
            registerCount = Math.Max(registerCount, method.Parameters.Count);
        if (registerCount > VmOpcodes.MaxFrameSize)
            return false;
        if (!VmStackAnalyzer.TryAnalyze(method, out var stackTypes))
            return false;
        var referenceCalls = method.MethodSig.RetType.RemovePinnedAndModifiers() is var returnType &&
                             (returnType.IsByRef || IsByRefLike(returnType)) ||
                             method.Parameters.Any(parameter => {
                                 var type = parameter.Type.RemovePinnedAndModifiers();
                                 return type.IsByRef || IsByRefLike(type);
                             }) ||
                             method.Body.Variables.Any(variable => {
                                 var type = variable.Type.RemovePinnedAndModifiers();
                                 return type.IsByRef || IsByRefLike(type);
                             }) ||
                             method.Body.Instructions.Any(i =>
                                 i.OpCode.Code is Code.Mkrefany or Code.Refanyval or Code.Ldflda or Code.Ldsflda
                                     or Code.Ldelema or Code.Unbox ||
                                 (i.OpCode.Code is Code.Call or Code.Callvirt) && i.Operand is IMethod target &&
                                 target.MethodSig.RetType.RemovePinnedAndModifiers().IsByRef ||
                                 i.OpCode.Code == Code.Calli && i.Operand is MethodSig site &&
                                 site.RetType.RemovePinnedAndModifiers().IsByRef ||
                                 (i.OpCode.Code is Code.Add or Code.Sub or Code.Add_Ovf_Un or Code.Sub_Ovf_Un) &&
                                 stackTypes[i].Any(type => type.RemovePinnedAndModifiers().IsByRef));
        var argumentCount = method.Parameters.Count + (hasArglist ? 1 : 0);
        if (VmFrame.GetCellCount(argumentCount, method.Body.Variables.Count, registerCount,
                method.Body.ExceptionHandlers.Count, method.Body.ExceptionHandlers.Count(h => h.IsFinally || h.IsFault),
                referenceCalls) < 0)
            return false;

        var map = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        if (opcodeScrambling) {
            for (var i = 255; i > 0; i--) {
                var j = rng.Next(i + 1);
                (map[i], map[j]) = (map[j], map[i]);
            }
        }

        var unmap = new byte[256];
        for (var i = 0; i < 256; i++)
            unmap[map[i]] = (byte)i;

        var bytes = new List<byte>();
        var offsets = new Dictionary<Instruction, int>();
        var fixups = new List<(int Position, Instruction Target)>();
        try {
            foreach (var instruction in method.Body.Instructions) {
                offsets[instruction] = bytes.Count;
                Emit(method, instruction, bytes, fixups, map, metadata, heights[positions[instruction]],
                    stackTypes[instruction]);
            }
        }
        catch (NotSupportedException error) {
            fallbackReason = error.Message;
            return false;
        }

        nativeTypes = CollectNativeTypes(method, metadata);
        accessTypes = CollectAccessTypes(method, stackTypes, nativeTypes);
        cellCount = VmFrame.GetCellCount(argumentCount, method.Body.Variables.Count, registerCount,
            method.Body.ExceptionHandlers.Count, unwindDepth, referenceCalls || nativeTypes.Count > 1,
            nativeTypes.Count - 1);
        if (cellCount < 0)
            return false;
        foreach (var (position, target) in fixups) {
            if (!offsets.TryGetValue(target, out var offset) || offset >= bytes.Count)
                return false;
            for (var shift = 0; shift < 32; shift += 8)
                bytes[position + shift / 8] = (byte)(offset >> shift);
        }

        var regions = new List<int>();
        foreach (var handler in method.Body.ExceptionHandlers) {
            var catchIndex = -1;
            if (handler.CatchType is { } catchType) {
                catchIndex = metadata.Count;
                metadata.Add(catchType);
            }

            regions.AddRange([
                Offset(handler.TryStart), Offset(handler.TryEnd), Offset(handler.HandlerStart),
                Offset(handler.HandlerEnd), handler.FilterStart is null ? -1 : Offset(handler.FilterStart),
                (int)handler.HandlerType, catchIndex
            ]);
            continue;
            int Offset(Instruction? boundary) => boundary is null ? bytes.Count : offsets[boundary];
        }

        var program = new byte[VmOpcodes.HeaderSize + regions.Count * 4 + bytes.Count];
        Array.Copy(unmap, program, 256);
        var header = new[] {
            VmOpcodes.Magic, registerCount, method.Body.Variables.Count, argumentCount,
            method.Body.ExceptionHandlers.Count
        };
        for (var i = 0; i < header.Length; i++)
        for (var shift = 0; shift < 32; shift += 8)
            program[256 + i * 4 + shift / 8] = (byte)(header[i] >> shift);
        for (var i = 0; i < regions.Count; i++)
        for (var shift = 0; shift < 32; shift += 8)
            program[VmOpcodes.HeaderSize + i * 4 + shift / 8] = (byte)(regions[i] >> shift);
        bytes.CopyTo(program, VmOpcodes.HeaderSize + regions.Count * 4);
        bytecode = VmRunner.Scramble(program);
        return true;

        void Merge(Instruction? target, int height) {
            if (target is null || !positions.TryGetValue(target, out var position)) {
                validStack = false;
                return;
            }

            if (heights[position] >= 0) {
                validStack &= heights[position] == height;
                return;
            }

            heights[position] = height;
            maxStack = Math.Max(maxStack, height);
            pending.Enqueue(position);
        }
    }

    private static List<TypeSig> CollectNativeTypes(MethodDef method, IEnumerable<object> metadata) {
        var module = method.Module;
        var result = new List<TypeSig> { module.Import(typeof(TypedReference)).ToTypeSig() };
        var comparer = new SigComparer();

        Visit(method.MethodSig.RetType);
        foreach (var parameter in method.Parameters)
            Visit(parameter.Type);
        foreach (var local in method.Body.Variables)
            Visit(local.Type);
        if (method.Body.Instructions.Any(instruction => instruction.OpCode.Code == Code.Arglist))
            Visit(new ValueTypeSig(module.CorLibTypes.GetTypeRef("System", nameof(RuntimeArgumentHandle))));
        foreach (var item in metadata) {
            switch (item) {
                case VmBridgeDescriptor bridge:
                    foreach (var input in bridge.Inputs)
                        Visit(input);
                    Visit(bridge.Result);
                    switch (bridge.Instruction.Operand) {
                        case IMethod { MethodSig: { } bridgeMethod } methodTarget: {
                            var bridgeArguments = Arguments(methodTarget.DeclaringType, methodTarget as MethodSpec);
                            Visit(bridgeMethod.RetType, bridgeArguments.Type, bridgeArguments.Method);
                            break;
                        }
                        case IField { FieldSig: { } bridgeField } fieldTarget: {
                            var bridgeArguments = Arguments(fieldTarget.DeclaringType);
                            Visit(bridgeField.Type, bridgeArguments.Type, bridgeArguments.Method);
                            break;
                        }
                        case ITypeDefOrRef bridgeType:
                            Visit(bridgeType.ToTypeSig());
                            break;
                    }

                    break;
                case IMethod { MethodSig: { } signature } target:
                    var methodArguments = Arguments(target.DeclaringType, target as MethodSpec);
                    Visit(signature.RetType, methodArguments.Type, methodArguments.Method);
                    Visit(target.DeclaringType.ToTypeSig(), methodArguments.Type, methodArguments.Method);
                    break;
                case IField { FieldSig: { } signature } target:
                    var fieldArguments = Arguments(target.DeclaringType);
                    Visit(signature.Type, fieldArguments.Type, fieldArguments.Method);
                    break;
                case ITypeDefOrRef type:
                    Visit(type.ToTypeSig());
                    break;
                case MethodSig signature:
                    Visit(signature.RetType);
                    break;
            }
        }

        return result;

        void Visit(TypeSig type, IList<TypeSig>? typeArguments = null, IList<TypeSig>? methodArguments = null) {
            type = VmSignatureResolver.Substitute(type, typeArguments, methodArguments).RemovePinnedAndModifiers();
            if (type.IsByRef) {
                Visit(type.Next, typeArguments, methodArguments);
                return;
            }

            if (VmTypeLayout.RequiresNative(method, type)) {
                if (!result.Any(existing => {
                            existing = existing.RemovePinnedAndModifiers();
                            var requested = type.RemovePinnedAndModifiers();
                            if (comparer.Equals(existing, requested))
                                return true;
                            if (existing is GenericInstSig || requested is GenericInstSig)
                                return false;
                            return existing is ClassOrValueTypeSig existingNamed &&
                                   requested is ClassOrValueTypeSig requestedNamed &&
                                   existingNamed.TypeDefOrRef.FullName == requestedNamed.TypeDefOrRef.FullName;
                        }
                    ))
                    result.Add(type);
            }

            switch (type) {
                case GenericInstSig instance:
                    foreach (var argument in instance.GenericArguments)
                        Visit(argument, typeArguments, methodArguments);
                    break;
                case PtrSig or SZArraySig or ArraySig:
                    Visit(type.Next, typeArguments, methodArguments);
                    break;
                case FnPtrSig pointer:
                    Visit(pointer.MethodSig.RetType, typeArguments, methodArguments);
                    foreach (var parameter in pointer.MethodSig.Params.Concat(pointer.MethodSig.ParamsAfterSentinel ??
                                                                              []))
                        Visit(parameter, typeArguments, methodArguments);
                    break;
            }
        }

        (IList<TypeSig>? Type, IList<TypeSig>? Method)
            Arguments(ITypeDefOrRef declaringType, MethodSpec? method = null) {
            var typeArguments = declaringType.ToTypeSig() is GenericInstSig declaringInstance
                ? declaringInstance.GenericArguments
                : null;
            return (typeArguments, method?.GenericInstMethodSig?.GenericArguments);
        }
    }

    private static List<TypeSig> CollectAccessTypes(MethodDef method,
        IReadOnlyDictionary<Instruction, TypeSig[]> stackTypes, IEnumerable<TypeSig> nativeTypes) {
        var result = new List<TypeSig>();
        var comparer = new SigComparer();

        Add(method.MethodSig.RetType);

        foreach (var parameter in method.Parameters)
            Add(parameter.Type);

        foreach (var local in method.Body.Variables)
            Add(local.Type);

        foreach (var types in stackTypes.Values) {
            foreach (var type in types)
                Add(type);
        }

        foreach (var type in nativeTypes)
            Add(type);

        return result;

        void Add(TypeSig signature) {
            var type = signature.RemovePinnedAndModifiers();
            if (type.IsByRef)
                type = type.Next.RemovePinnedAndModifiers();
            if (type.ElementType == ElementType.Void || result.Any(existing => comparer.Equals(existing, type)))
                return;
            result.Add(type);
        }
    }

    private static void Emit(MethodDef method, Instruction instruction, List<byte> bytes,
        List<(int, Instruction)> fixups, byte[] map, List<object> metadata, int height, TypeSig[] stackTypes) {
        void Op(byte op) {
            instruction.CalculateStackUsage(out _, out var inputs);
            if (instruction.OpCode.Code == Code.Ret)
                inputs = method.MethodSig.RetType.ElementType == ElementType.Void ? 0 : 1;
            bytes.Add(map[op]);
            Int(height < 0 || inputs < 0 ? 0 : height - inputs);
        }

        void Int(int value) {
            for (var shift = 0; shift < 32; shift += 8)
                bytes.Add((byte)(value >> shift));
        }

        void Target(Instruction target) {
            fixups.Add((bytes.Count, target));
            Int(0);
        }

        if (instruction.OpCode.Code is Code.And or Code.Or or Code.Xor or Code.Shl or Code.Shr or Code.Shr_Un
            or Code.Not or
            Code.Add or Code.Sub or Code.Mul or Code.Div or Code.Rem or Code.Neg or Code.Add_Ovf or Code.Sub_Ovf
            or Code.Mul_Ovf or
            Code.Add_Ovf_Un or Code.Sub_Ovf_Un or Code.Mul_Ovf_Un or Code.Div_Un or Code.Rem_Un) {
            instruction.CalculateStackUsage(out _, out var arithmeticInputs);
            if (stackTypes.Skip(stackTypes.Length - arithmeticInputs)
                .Any(type => type.RemovePinnedAndModifiers().IsByRef)) {
                if (instruction.OpCode.Code is not (Code.Add or Code.Sub or Code.Add_Ovf_Un or Code.Sub_Ovf_Un))
                    throw new NotSupportedException("managed-reference arithmetic not supported.");
                Op(VmOpcodes.Operation);
                Int(metadata.Count);
                metadata.Add(VmBridgeEmitter.Describe(method, instruction, stackTypes));
                Int(arithmeticInputs);
                return;
            }

            Op(instruction.OpCode.Code switch {
                Code.And => VmOpcodes.BitAnd,
                Code.Or => VmOpcodes.BitOr,
                Code.Xor => VmOpcodes.BitXor,
                Code.Shl => VmOpcodes.ShiftLeft,
                Code.Shr => VmOpcodes.ShiftRight,
                Code.Shr_Un => VmOpcodes.ShiftRightUnsigned,
                Code.Not => VmOpcodes.BitNot,
                Code.Add => VmOpcodes.Add,
                Code.Sub => VmOpcodes.Subtract,
                Code.Mul => VmOpcodes.Multiply,
                Code.Div => VmOpcodes.Divide,
                Code.Rem => VmOpcodes.Remainder,
                Code.Neg => VmOpcodes.Negate,
                Code.Add_Ovf => VmOpcodes.AddChecked,
                Code.Sub_Ovf => VmOpcodes.SubtractChecked,
                Code.Mul_Ovf => VmOpcodes.MultiplyChecked,
                Code.Add_Ovf_Un => VmOpcodes.AddUnsignedChecked,
                Code.Sub_Ovf_Un => VmOpcodes.SubtractUnsignedChecked,
                Code.Mul_Ovf_Un => VmOpcodes.MultiplyUnsignedChecked,
                Code.Div_Un => VmOpcodes.DivideUnsigned,
                _ => VmOpcodes.RemainderUnsigned,
            });
            return;
        }

        switch (instruction.OpCode.Code) {
            case Code.Nop:
                break;
            case Code.Ldarg_0 or Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3 or Code.Ldarg_S or Code.Ldarg:
            case Code.Starg or Code.Starg_S: {
                var index = instruction.OpCode.Code switch {
                    Code.Ldarg_0 => 0, Code.Ldarg_1 => 1, Code.Ldarg_2 => 2, Code.Ldarg_3 => 3,
                    _ => ((Parameter)instruction.Operand).Index,
                };
                if ((uint)index >= (uint)method.Parameters.Count)
                    throw new NotSupportedException("arg outside signature.");
                Op(instruction.OpCode.Code is Code.Starg or Code.Starg_S ? VmOpcodes.Starg : VmOpcodes.Ldarg);
                Int(index);
                break;
            }
            case Code.Ldarga or Code.Ldarga_S:
                Op(VmOpcodes.Ldarga);
                Int(((Parameter)instruction.Operand).Index);
                break;
            case Code.Ldloca or Code.Ldloca_S:
                Op(VmOpcodes.Ldloca);
                Int(((Local)instruction.Operand).Index);
                break;
            case Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 or Code.Ldloc_S or Code.Ldloc:
            case Code.Stloc_0 or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3 or Code.Stloc_S or Code.Stloc: {
                var index = instruction.OpCode.Code switch {
                    Code.Ldloc_0 or Code.Stloc_0 => 0, Code.Ldloc_1 or Code.Stloc_1 => 1,
                    Code.Ldloc_2 or Code.Stloc_2 => 2, Code.Ldloc_3 or Code.Stloc_3 => 3,
                    _ => ((Local)instruction.Operand).Index,
                };
                if ((uint)index >= (uint)method.Body.Variables.Count)
                    throw new NotSupportedException("local out of frame.");
                Op(instruction.IsStloc() ? VmOpcodes.Stloc : VmOpcodes.Ldloc);
                Int(index);
                break;
            }
            case Code.Ldc_I4_M1 or Code.Ldc_I4_0 or Code.Ldc_I4_1 or Code.Ldc_I4_2 or Code.Ldc_I4_3 or
                Code.Ldc_I4_4 or Code.Ldc_I4_5 or Code.Ldc_I4_6 or Code.Ldc_I4_7 or Code.Ldc_I4_8 or Code.Ldc_I4_S
                or Code.Ldc_I4:
                Op(VmOpcodes.Ldc);
                Int(instruction.GetLdcI4Value());
                break;
            case Code.Dup:
                Op(VmOpcodes.Dup);
                break;
            case Code.Pop:
                Op(VmOpcodes.Pop);
                break;
            case Code.Ret:
                Op(VmOpcodes.Ret);
                break;
            case Code.Endfinally:
                Op(VmOpcodes.Endfinally);
                break;
            case Code.Endfilter:
                Op(VmOpcodes.Endfilter);
                break;
            case Code.Rethrow:
                Op(VmOpcodes.Rethrow);
                break;
            case Code.Leave or Code.Leave_S:
                Op(VmOpcodes.Leave);
                Target((Instruction)instruction.Operand);
                break;
            case Code.Br or Code.Br_S:
                Op(VmOpcodes.Br);
                Target((Instruction)instruction.Operand);
                break;
            case Code.Brtrue or Code.Brtrue_S:
                Op(VmOpcodes.BrTrue);
                Target((Instruction)instruction.Operand);
                break;
            case Code.Brfalse or Code.Brfalse_S:
                Op(VmOpcodes.BrFalse);
                Target((Instruction)instruction.Operand);
                break;
            case Code.Switch:
                var targets = (IList<Instruction>)instruction.Operand;
                Op(VmOpcodes.Switch);
                Int(targets.Count);
                foreach (var target in targets)
                    Target(target);
                break;
            case Code.Arglist:
                if (!method.MethodSig.IsVarArg)
                    throw new NotSupportedException("arglist outside varargs method.");
                Op(VmOpcodes.Ldarg);
                Int(method.Parameters.Count);
                break;
            case Code.Jmp: {
                var target = (IMethod)instruction.Operand;
                if (target.MethodSig is null || !new SigComparer().Equals(method.MethodSig, target.MethodSig) ||
                    method.Body.ExceptionHandlers.Any(handler => instruction.Offset >= handler.TryStart.Offset &&
                                                                 instruction.Offset < (handler.TryEnd?.Offset ??
                                                                     uint.MaxValue) ||
                                                                 instruction.Offset >= handler.HandlerStart.Offset &&
                                                                 instruction.Offset < (handler.HandlerEnd?.Offset ??
                                                                     uint.MaxValue)))
                    throw new NotSupportedException("invalid jmp target.");
                for (var index = 0; index < method.Parameters.Count; index++) {
                    bytes.Add(map[VmOpcodes.Ldarg]);
                    Int(index);
                    Int(index);
                }

                bytes.Add(map[VmOpcodes.Operation]);
                Int(0);
                Int(metadata.Count);
                var call = OpCodes.Call.ToInstruction(target);
                metadata.Add(VmBridgeEmitter.Describe(method, call,
                    method.Parameters.Select(parameter => parameter.Type).ToArray()));
                Int(method.Parameters.Count);
                bytes.Add(map[VmOpcodes.Ret]);
                Int(0);
                break;
            }
            case Code.Constrained or Code.Volatile or Code.Readonly or Code.Unaligned or Code.Tailcall:
                break;
            case Code.Localloc:
                Op(VmOpcodes.Localloc);
                break;
            default:
                if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch) {
                    Op(VmOpcodes.Operation);
                    Int(metadata.Count);
                    metadata.Add(VmBridgeEmitter.Describe(method, instruction, stackTypes));
                    instruction.CalculateStackUsage(out _, out var branchInputs);
                    Int(branchInputs);
                    Op(VmOpcodes.BrTrue);
                    Target((Instruction)instruction.Operand);
                    break;
                }

                if (!VmBridgeEmitter.IsSupported(instruction.OpCode.Code))
                    throw new NotSupportedException(instruction.OpCode.Name);
                if (instruction.Operand is IMethod { MethodSig: { } sig } &&
                    instruction.OpCode.Code is Code.Call or Code.Callvirt or Code.Newobj &&
                    (!SupportsType(sig.RetType, true) ||
                     !sig.Params.All(t => SupportsType(t, true)) ||
                     sig.ParamsAfterSentinel?.All(t => SupportsType(t, true)) == false))
                    throw new NotSupportedException("call signature unsupported.");
                if (instruction.Operand is ITypeDefOrRef type && !SupportsType(type.ToTypeSig(), true))
                    throw new NotSupportedException("operand type unsupported.");
                if (instruction.Operand is IField { FieldSig: { } fieldSig } && !SupportsType(fieldSig.Type, true))
                    throw new NotSupportedException("field type unsupported.");
                if (instruction.Operand is MethodSig site && (!SupportsType(site.RetType, true) ||
                                                              !site.Params.All(t => SupportsType(t, true)) ||
                                                              site.ParamsAfterSentinel?.All(t =>
                                                                  SupportsType(t, true)) == false || site.Generic))
                    throw new NotSupportedException("indirect call signature unsupported.");
                Op(VmOpcodes.Operation);
                Int(metadata.Count);
                metadata.Add(VmBridgeEmitter.Describe(method, instruction, stackTypes));
                instruction.CalculateStackUsage(out _, out var inputCount);
                if ((uint)inputCount > VmOpcodes.MaxFrameSize)
                    throw new NotSupportedException("bad stack consumption.");
                Int(inputCount);
                break;
        }
    }

    private static void ReplaceBody(ModuleDef module, MethodDef method, InjectResult runtime, MethodDef host,
        string program,
        List<object> metadata, IReadOnlyList<TypeSig> nativeTypes,
        IReadOnlyList<VmReferenceAccessDescriptor> accessors, bool runtimeErrors, long scrambleKey, long key0,
        long key1,
        int encryptionRounds, long bindingKey) {
        var returnType = method.MethodSig.RetType.RemovePinnedAndModifiers();
        var hasArglist = method.Body.Instructions.Any(instruction => instruction.OpCode.Code == Code.Arglist);
        var arglistType = new ValueTypeSig(module.CorLibTypes.GetTypeRef("System", nameof(RuntimeArgumentHandle)));
        var createFrame = runtime.ResolveMethod(typeof(VmRunner).GetMethod(nameof(VmRunner.CreateFrame))!)!;
        var scopeDefinition = createFrame.MethodSig.Params[^1].RemovePinnedAndModifiers().ToTypeDefOrRef();
        var referenceType = scopeDefinition.DeclaringType!.ResolveTypeDef()!;
        var createScope = referenceType.Methods.Single(candidate =>
            candidate.Name == nameof(VmReference.CreateScope) && candidate.MethodSig.Params.Count == 0);
        var pointer = referenceType.Methods.Single(candidate =>
            candidate.Name == nameof(VmReference.Pointer) && candidate.MethodSig.Params.Count == 4);

        var operationConstructor = runtime.ResolveMethod(typeof(VmOperation).GetConstructors().Single())!;
        var nativeConstructor = runtime.ResolveMethod(typeof(VmNativeValue).GetConstructors().Single())!;
        var pinsConstructor = runtime.ResolveMethod(typeof(VmPins).GetConstructor(Type.EmptyTypes)!)!;
        var disposePins = runtime.ResolveMethod(typeof(VmPins).GetMethod(nameof(VmPins.Dispose))!)!;

        var resultCell = runtime.ResolveField(typeof(VmFrame).GetField(nameof(VmFrame.ResultCell))!)!;
        var diagnostics = runtime.ResolveField(typeof(VmCrash).GetField(nameof(VmCrash.Diagnostics))!)!;

        var body = new CilBody { InitLocals = true };

        var argv = new Local(new SZArraySig(module.CorLibTypes.Object));
        var operands = new Local(new SZArraySig(module.CorLibTypes.Object));
        var nativeTypeArray = new Local(new SZArraySig(module.Import(typeof(Type)).ToTypeSig()));
        var nativeBankArray = new Local(new SZArraySig(module.CorLibTypes.Int32));

        var pins = new Local(runtime.ResolveType(typeof(VmPins))!.ToTypeSig());
        var frame = new Local(runtime.ResolveType(typeof(VmFrame))!.ToTypeSig());
        var scope = new Local(createFrame.MethodSig.Params[^1]);

        body.Variables.Add(argv);
        body.Variables.Add(operands);
        body.Variables.Add(nativeTypeArray);
        body.Variables.Add(nativeBankArray);
        body.Variables.Add(pins);
        body.Variables.Add(frame);
        body.Variables.Add(scope);
        var nativeBanks = new[] { method.MethodSig.RetType }
            .Concat(method.Parameters.Select(parameter => parameter.Type))
            .Concat(hasArglist ? [arglistType] : [])
            .Concat(method.Body.Variables.Select(variable => variable.Type))
            .Select(type => VmTypeLayout.Bank(method, type, nativeTypes)).ToArray();
        var resultStorage = nativeBanks[0] >= 0 ? new Local(returnType) : null;
        if (resultStorage is not null)
            body.Variables.Add(resultStorage);
        TypeSig hostResultType = module.CorLibTypes.Object;
        if (returnType.IsByRef) {
            var elementType = returnType.Next.RemovePinnedAndModifiers();
            var storageType = elementType.ElementType is ElementType.Ptr or ElementType.FnPtr
                ? module.CorLibTypes.IntPtr
                : elementType;
            hostResultType = new ByRefSig(storageType);
        }

        var hostResult = new Local(hostResultType);
        body.Variables.Add(hostResult);
        var il = body.Instructions;
        il.Add((runtimeErrors ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0).ToInstruction());
        il.Add(OpCodes.Stsfld.ToInstruction(diagnostics));
        var getType = module.Import(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
        var actionDefinition = module.Import(typeof(Action<,>));

        IMethod GeneratedMethod(MethodDef generated) {
            IMethodDefOrRef reference = generated;
            if (method.DeclaringType.GenericParameters.Count == 0)
                return method.GenericParameters.Count == 0
                    ? reference
                    : new MethodSpecUser(reference, new GenericInstMethodSig(method.GenericParameters
                        .Select(TypeSig (parameter) => new GenericMVar(parameter.Number)).ToArray()));

            {
                var declaringType = new GenericInstSig((ClassOrValueTypeSig)method.DeclaringType.ToTypeSig(),
                    method.DeclaringType.GenericParameters
                        .Select(TypeSig (parameter) => new GenericVar(parameter.Number)).ToArray());
                reference = new MemberRefUser(module, generated.Name, generated.MethodSig,
                    new TypeSpecUser(declaringType));
            }

            return method.GenericParameters.Count == 0
                ? reference
                : new MethodSpecUser(reference, new GenericInstMethodSig(method.GenericParameters
                    .Select(TypeSig (parameter) => new GenericMVar(parameter.Number)).ToArray()));
        }

        MemberRef DelegateConstructor(TypeSig delegateType) => new MemberRefUser(module, ".ctor",
            MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.Object, module.CorLibTypes.IntPtr),
            new TypeSpecUser(delegateType));

        var nativeCopyType = new GenericInstSig(new ClassSig(actionDefinition),
            module.CorLibTypes.IntPtr, module.CorLibTypes.IntPtr);

        void EmitNativeCopy(TypeSig type) {
            var bank = VmTypeLayout.Bank(method, type, nativeTypes);
            var accessor = accessors.FirstOrDefault(candidate => candidate.Native &&
                                                                 VmTypeLayout.Bank(method, candidate.Type,
                                                                     nativeTypes) == bank)?.NativeCopy;
            if (accessor is null)
                throw new NotSupportedException($"Missing native copy accessor for {type.FullName}");
            il.Add(OpCodes.Ldnull.ToInstruction());
            il.Add(OpCodes.Ldftn.ToInstruction(GeneratedMethod(accessor)));
            il.Add(OpCodes.Newobj.ToInstruction(DelegateConstructor(nativeCopyType)));
        }

        var vmReferenceType = referenceType.ToTypeSig();
        var addressDefinition = runtime.ResolveType(typeof(VmAddress<>))!;
        var register = referenceType.Methods.Single(candidate => candidate.Name == nameof(VmReference.Register) &&
                                                                 candidate.GenericParameters.Count == 1 &&
                                                                 candidate.MethodSig.Params.Count == 4);
        var registerNative = referenceType.Methods.Single(candidate =>
            candidate.Name == nameof(VmReference.RegisterNative) && candidate.MethodSig.Params.Count == 5);
        il.Add(OpCodes.Call.ToInstruction(createScope));
        il.Add(OpCodes.Stloc.ToInstruction(scope));
        foreach (var accessor in accessors) {
            if (accessor.Native) {
                il.Add(OpCodes.Ldloc.ToInstruction(scope));
                il.Add(OpCodes.Ldtoken.ToInstruction(accessor.Type.ToTypeDefOrRef()));
                il.Add(OpCodes.Call.ToInstruction(getType));
                var cellDelegate = new GenericInstSig(new ClassSig(module.Import(typeof(Func<,,>))),
                    module.CorLibTypes.Object, module.CorLibTypes.Int32, module.CorLibTypes.IntPtr);
                il.Add(OpCodes.Ldnull.ToInstruction());
                il.Add(OpCodes.Ldftn.ToInstruction(GeneratedMethod(accessor.Cell)));
                il.Add(OpCodes.Newobj.ToInstruction(DelegateConstructor(cellDelegate)));
                var copyDelegate = new GenericInstSig(new ClassSig(actionDefinition), vmReferenceType,
                    module.CorLibTypes.IntPtr);
                il.Add(OpCodes.Ldnull.ToInstruction());
                il.Add(OpCodes.Ldftn.ToInstruction(GeneratedMethod(accessor.Copy)));
                il.Add(OpCodes.Newobj.ToInstruction(DelegateConstructor(copyDelegate)));
                var nativeCopyDelegate = new GenericInstSig(new ClassSig(actionDefinition),
                    module.CorLibTypes.IntPtr, module.CorLibTypes.IntPtr);
                il.Add(OpCodes.Ldnull.ToInstruction());
                il.Add(OpCodes.Ldftn.ToInstruction(GeneratedMethod(accessor.NativeCopy!)));
                il.Add(OpCodes.Newobj.ToInstruction(DelegateConstructor(nativeCopyDelegate)));
                il.Add(OpCodes.Call.ToInstruction(registerNative));
            }
            else {
                il.Add(OpCodes.Ldloc.ToInstruction(scope));
                il.Add(OpCodes.Ldtoken.ToInstruction(accessor.Type.ToTypeDefOrRef()));
                il.Add(OpCodes.Call.ToInstruction(getType));
                var addressDelegate = new GenericInstSig((ClassOrValueTypeSig)addressDefinition.ToTypeSig(),
                    accessor.StorageType);
                il.Add(OpCodes.Ldnull.ToInstruction());
                il.Add(OpCodes.Ldftn.ToInstruction(GeneratedMethod(accessor.Cell)));
                il.Add(OpCodes.Newobj.ToInstruction(DelegateConstructor(addressDelegate)));
                var copyDelegate = new GenericInstSig(new ClassSig(actionDefinition), vmReferenceType,
                    module.CorLibTypes.IntPtr);
                il.Add(OpCodes.Ldnull.ToInstruction());
                il.Add(OpCodes.Ldftn.ToInstruction(GeneratedMethod(accessor.Copy)));
                il.Add(OpCodes.Newobj.ToInstruction(DelegateConstructor(copyDelegate)));
                il.Add(OpCodes.Call.ToInstruction(new MethodSpecUser(register,
                    new GenericInstMethodSig(accessor.StorageType))));
            }
        }

        var storageLocals = new List<Local>();
        il.Add(OpCodes.Ldc_I4.ToInstruction(method.Parameters.Count + (hasArglist ? 1 : 0)));
        il.Add(OpCodes.Newarr.ToInstruction(module.CorLibTypes.Object.TypeDefOrRef));
        il.Add(OpCodes.Stloc.ToInstruction(argv));
        foreach (var parameter in method.Parameters) {
            var parameterType = parameter.Type.RemovePinnedAndModifiers();
            var nativeBank = nativeBanks[1 + parameter.Index];
            il.Add(OpCodes.Ldloc.ToInstruction(argv));
            il.Add(OpCodes.Ldc_I4.ToInstruction(parameter.Index));
            il.Add(OpCodes.Ldarg.ToInstruction(parameter));
            if (parameterType.IsByRef) {
                var storage = new Local(new PinnedSig(parameterType));
                body.Variables.Add(storage);
                storageLocals.Add(storage);
                il.Add(OpCodes.Stloc.ToInstruction(storage));
                il.Add(OpCodes.Ldloc.ToInstruction(scope));
                il.Add(OpCodes.Ldloc.ToInstruction(storage));
                il.Add(OpCodes.Conv_I.ToInstruction());
                il.Add(OpCodes.Ldtoken.ToInstruction(parameterType.Next.ToTypeDefOrRef()));
                il.Add(OpCodes.Call.ToInstruction(getType));
                il.Add(OpCodes.Ldc_I4_M1.ToInstruction());
                il.Add(OpCodes.Call.ToInstruction(pointer));
            }
            else if (nativeBank >= 0) {
                var storage = new Local(parameterType);
                body.Variables.Add(storage);
                storageLocals.Add(storage);
                il.Add(OpCodes.Stloc.ToInstruction(storage));
                il.Add(OpCodes.Ldloca.ToInstruction(storage));
                il.Add(OpCodes.Conv_I.ToInstruction());
                il.Add(OpCodes.Ldtoken.ToInstruction(parameterType.ToTypeDefOrRef()));
                il.Add(OpCodes.Call.ToInstruction(getType));
                il.Add(OpCodes.Ldc_I4.ToInstruction(nativeBank));
                EmitNativeCopy(parameterType);
                il.Add(OpCodes.Newobj.ToInstruction(nativeConstructor));
            }
            else if (parameterType.ElementType is ElementType.Ptr or ElementType.FnPtr) {
                il.Add(OpCodes.Conv_I.ToInstruction());
                il.Add(OpCodes.Box.ToInstruction(module.CorLibTypes.IntPtr.TypeDefOrRef));
            }
            else if (parameterType.IsValueType || parameterType is GenericSig)
                il.Add(OpCodes.Box.ToInstruction(parameterType.ToTypeDefOrRef()));

            il.Add(OpCodes.Stelem_Ref.ToInstruction());
        }

        if (hasArglist) {
            var index = method.Parameters.Count;
            var nativeBank = nativeBanks[1 + index];
            var storage = new Local(arglistType);
            body.Variables.Add(storage);
            storageLocals.Add(storage);
            il.Add(OpCodes.Ldloc.ToInstruction(argv));
            il.Add(OpCodes.Ldc_I4.ToInstruction(index));
            il.Add(OpCodes.Arglist.ToInstruction());
            il.Add(OpCodes.Stloc.ToInstruction(storage));
            il.Add(OpCodes.Ldloca.ToInstruction(storage));
            il.Add(OpCodes.Conv_I.ToInstruction());
            il.Add(OpCodes.Ldtoken.ToInstruction(arglistType.ToTypeDefOrRef()));
            il.Add(OpCodes.Call.ToInstruction(getType));
            il.Add(OpCodes.Ldc_I4.ToInstruction(nativeBank));
            EmitNativeCopy(arglistType);
            il.Add(OpCodes.Newobj.ToInstruction(nativeConstructor));
            il.Add(OpCodes.Stelem_Ref.ToInstruction());
        }

        var bridgeDelegateConstructor =
            module.Import(typeof(Func<object?[], IntPtr, object?>).GetConstructors().Single());
        il.Add(OpCodes.Ldc_I4.ToInstruction(metadata.Count));
        il.Add(OpCodes.Newarr.ToInstruction(module.CorLibTypes.Object.TypeDefOrRef));
        il.Add(OpCodes.Stloc.ToInstruction(operands));

        void Value(object operand) {
            switch (operand) {
                case VmBridgeDescriptor bridge:
                    Value(bridge.Result.ToTypeDefOrRef());
                    var resultBank = bridge.Result.RemovePinnedAndModifiers().IsByRef
                        ? 0
                        : VmTypeLayout.Bank(method, bridge.Result, nativeTypes);
                    il.Add(OpCodes.Ldc_I4.ToInstruction(resultBank));
                    il.Add(OpCodes.Ldnull.ToInstruction());
                    il.Add(OpCodes.Ldftn.ToInstruction(GeneratedMethod(bridge.Method!)));
                    il.Add(OpCodes.Newobj.ToInstruction(bridgeDelegateConstructor));
                    var nativeAccessor = resultBank >= 0
                        ? accessors.FirstOrDefault(accessor => accessor.Native &&
                                                               VmTypeLayout.Bank(method, accessor.Type, nativeTypes) ==
                                                               resultBank)?.NativeCopy
                        : null;

                    il.Add(OpCodes.Ldnull.ToInstruction());

                    if (nativeAccessor is not null) {
                        il.Add(OpCodes.Ldftn.ToInstruction(GeneratedMethod(nativeAccessor)));
                        il.Add(OpCodes.Newobj.ToInstruction(DelegateConstructor(new GenericInstSig(
                            new ClassSig(actionDefinition),
                            module.CorLibTypes.IntPtr, module.CorLibTypes.IntPtr))));
                    }

                    il.Add(OpCodes.Newobj.ToInstruction(operationConstructor));
                    break;
                case TypeSpec { TypeSig: ModifierSig or PinnedSig } modified:
                    Value(modified.TypeSig.RemovePinnedAndModifiers().ToTypeDefOrRef());
                    break;
                case TypeSpec { TypeSig: ByRefSig or PtrSig } shaped:
                    il.Add(OpCodes.Ldtoken.ToInstruction(shaped));
                    il.Add(OpCodes.Call.ToInstruction(getType));
                    break;
                case ITypeDefOrRef type:
                    il.Add(OpCodes.Ldtoken.ToInstruction(type));
                    il.Add(OpCodes.Call.ToInstruction(getType));
                    break;
                default:
                    throw new NotSupportedException("bad metadata operand.");
            }
        }

        for (var i = 0; i < metadata.Count; i++) {
            il.Add(OpCodes.Ldloc.ToInstruction(operands));
            il.Add(OpCodes.Ldc_I4.ToInstruction(i));
            Value(metadata[i]);
            il.Add(OpCodes.Stelem_Ref.ToInstruction());
        }

        il.Add(OpCodes.Ldc_I4.ToInstruction(nativeTypes.Count));
        il.Add(OpCodes.Newarr.ToInstruction(module.Import(typeof(Type))));
        il.Add(OpCodes.Stloc.ToInstruction(nativeTypeArray));
        for (var i = 0; i < nativeTypes.Count; i++) {
            il.Add(OpCodes.Ldloc.ToInstruction(nativeTypeArray));
            il.Add(OpCodes.Ldc_I4.ToInstruction(i));
            Value(nativeTypes[i].ToTypeDefOrRef());
            il.Add(OpCodes.Stelem_Ref.ToInstruction());
        }

        il.Add(OpCodes.Ldc_I4.ToInstruction(nativeBanks.Length));
        il.Add(OpCodes.Newarr.ToInstruction(module.CorLibTypes.Int32.TypeDefOrRef));
        il.Add(OpCodes.Stloc.ToInstruction(nativeBankArray));
        for (var i = 0; i < nativeBanks.Length; i++) {
            il.Add(OpCodes.Ldloc.ToInstruction(nativeBankArray));
            il.Add(OpCodes.Ldc_I4.ToInstruction(i));
            il.Add(OpCodes.Ldc_I4.ToInstruction(nativeBanks[i]));
            il.Add(OpCodes.Stelem_I4.ToInstruction());
        }

        il.Add(OpCodes.Newobj.ToInstruction(pinsConstructor));
        il.Add(OpCodes.Stloc.ToInstruction(pins));
        var start = OpCodes.Ldstr.ToInstruction(program);
        il.Add(start);
        il.Add(OpCodes.Ldloc.ToInstruction(argv));
        il.Add(OpCodes.Ldloc.ToInstruction(operands));
        il.Add(OpCodes.Ldloc.ToInstruction(pins));
        il.Add(OpCodes.Ldloc.ToInstruction(nativeTypeArray));
        il.Add(OpCodes.Ldloc.ToInstruction(nativeBankArray));
        il.Add(OpCodes.Ldc_I8.ToInstruction(scrambleKey));
        il.Add(OpCodes.Ldc_I8.ToInstruction(key0));
        il.Add(OpCodes.Ldc_I8.ToInstruction(key1));
        il.Add(OpCodes.Ldc_I4.ToInstruction(encryptionRounds));
        il.Add(OpCodes.Ldc_I8.ToInstruction(bindingKey));
        il.Add(OpCodes.Ldloc.ToInstruction(scope));
        il.Add(OpCodes.Call.ToInstruction(createFrame));
        il.Add(OpCodes.Stloc.ToInstruction(frame));
        if (resultStorage is not null) {
            il.Add(OpCodes.Ldloc.ToInstruction(frame));
            il.Add(OpCodes.Ldloca.ToInstruction(resultStorage));
            il.Add(OpCodes.Conv_I.ToInstruction());
            il.Add(OpCodes.Stfld.ToInstruction(resultCell));
        }

        il.Add(OpCodes.Ldloc.ToInstruction(frame));
        il.Add(OpCodes.Call.ToInstruction(GeneratedMethod(host)));
        il.Add(OpCodes.Stloc.ToInstruction(hostResult));
        var finallyStart = OpCodes.Ldloc.ToInstruction(pins);
        var completed = OpCodes.Nop.ToInstruction();
        il.Add(OpCodes.Leave.ToInstruction(completed));
        il.Add(finallyStart);
        il.Add(OpCodes.Callvirt.ToInstruction(disposePins));
        il.Add(OpCodes.Endfinally.ToInstruction());
        il.Add(completed);
        body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally) {
            TryStart = start,
            TryEnd = finallyStart,
            HandlerStart = finallyStart,
            HandlerEnd = completed,
        });
        foreach (var storage in storageLocals) {
            il.Add(OpCodes.Ldloc.ToInstruction(storage));
            il.Add(OpCodes.Pop.ToInstruction());
        }

        if (resultStorage is not null) {
            il.Add(OpCodes.Ldloc.ToInstruction(resultStorage));
        }
        else
            switch (returnType.ElementType) {
                case ElementType.Void:
                    il.Add(OpCodes.Ldloc.ToInstruction(hostResult));
                    break;
                case ElementType.Ptr or ElementType.FnPtr:
                    il.Add(OpCodes.Ldloc.ToInstruction(hostResult));
                    il.Add(OpCodes.Unbox_Any.ToInstruction(module.CorLibTypes.IntPtr.TypeDefOrRef));
                    il.Add(OpCodes.Conv_U.ToInstruction());
                    break;
                default: {
                    if (!returnType.IsByRef) {
                        il.Add(OpCodes.Ldloc.ToInstruction(hostResult));
                        il.Add((returnType.IsValueType || returnType is GenericSig
                                ? OpCodes.Unbox_Any
                                : OpCodes.Castclass)
                            .ToInstruction(returnType.ToTypeDefOrRef()));
                    }
                    else
                        il.Add(OpCodes.Ldloc.ToInstruction(hostResult));

                    break;
                }
            }

        if (returnType.ElementType == ElementType.Void)
            il.Add(OpCodes.Pop.ToInstruction());
        il.Add(OpCodes.Ret.ToInstruction());
        body.OptimizeMacros();
        body.OptimizeBranches();
        method.Body = body;
    }
}