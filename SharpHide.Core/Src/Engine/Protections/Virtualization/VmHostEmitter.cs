using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Injection;
using SharpHide.Vm;

namespace SharpHide.Core.Engine.Protections;

internal static class VmHostEmitter {
    public static MethodDef Create(ModuleDef module, MethodDef method, InjectResult runtime,
        IReadOnlyList<TypeSig> nativeTypes, int cellCount, int unwindDepth) {
        var returnType = method.MethodSig.RetType.RemovePinnedAndModifiers();
        TypeSig hostReturnType = module.CorLibTypes.Object;
        if (returnType.IsByRef) {
            var elementType = returnType.Next.RemovePinnedAndModifiers();
            var storageType = elementType.ElementType is ElementType.Ptr or ElementType.FnPtr
                ? module.CorLibTypes.IntPtr
                : elementType;
            hostReturnType = new ByRefSig(storageType);
        }

        var frameType = runtime.ResolveType(typeof(VmFrame))!.ToTypeSig();
        var host = new MethodDefUser(UniqueName(method), MethodSig.CreateStatic(hostReturnType, frameType),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig) {
            Body = new CilBody { InitLocals = true },
        };
        VmTypeLayout.CopyGenericParameters(method, host);
        method.DeclaringType.Methods.Add(host);
        EmitBody(module, host, runtime, nativeTypes, cellCount, unwindDepth,
            method.Body.ExceptionHandlers.Count != 0, returnType);
        return host;
    }

    private static string UniqueName(MethodDef method) {
        var prefix = method.Name + "$vmhost";
        var name = prefix;
        for (var suffix = 2; method.DeclaringType.Methods.Any(candidate => candidate.Name == name); suffix++)
            name = prefix + suffix;
        return name;
    }

    private static void EmitBody(ModuleDef module, MethodDef host, InjectResult runtime,
        IReadOnlyList<TypeSig> nativeTypes, int cellCount, int unwindDepth, bool hasExceptionRegions,
        TypeSig returnType) {
        var body = host.Body;
        var il = body.Instructions;
        MethodDef FrameMethod(string name) => runtime.ResolveMethod(typeof(VmFrame).GetMethod(name)!)!;
        var attach = FrameMethod(nameof(VmFrame.AttachCells));
        var step = FrameMethod(nameof(VmFrame.Step));
        var ready = FrameMethod(nameof(VmFrame.Ready));
        var needsAllocation = FrameMethod(nameof(VmFrame.NeedsAllocation));
        var allocationSize = FrameMethod(nameof(VmFrame.AllocationSize));
        var allocated = FrameMethod(nameof(VmFrame.Allocated));
        var beginSearch = FrameMethod(nameof(VmFrame.BeginSearch));
        var searchStep = FrameMethod(nameof(VmFrame.SearchStep));
        var hasHandler = FrameMethod(nameof(VmFrame.HasHandler));
        var catchMethod = FrameMethod(nameof(VmFrame.Catch));
        var beginEscape = FrameMethod(nameof(VmFrame.BeginEscape));
        var errorMethod = FrameMethod(nameof(VmFrame.Error));
        var resumeTransfer = FrameMethod(nameof(VmFrame.ResumeTransfer));
        var pinRoot = FrameMethod(nameof(VmFrame.PinRoot));
        var nextRoot = FrameMethod(nameof(VmFrame.NextRoot));
        var root = runtime.ResolveMethod(typeof(VmFrame).GetMethod(nameof(VmFrame.Root))!)!;
        var referenceResult = runtime.ResolveMethod(typeof(VmFrame).GetMethod(nameof(VmFrame.ReferenceResult))!)!;
        var frameResult = runtime.ResolveField(typeof(VmFrame).GetField(nameof(VmFrame.Result))!)!;
        var referenceType = runtime.ResolveType(typeof(VmReference))!;
        var referenceBytes = runtime.ResolveMethod(typeof(VmReference).GetMethod(nameof(VmReference.GetBytes))!)!;
        var valueResult = FrameMethod(nameof(VmFrame.ValueResult));
        var transferType = runtime.ResolveType(typeof(VmTransfer))!;
        var transferConstructor = runtime.ResolveMethod(typeof(VmTransfer).GetConstructors().Single())!;
        var crash = runtime.ResolveMethod(typeof(VmCrash).GetMethods().Single(method =>
            method.Name == nameof(VmCrash.Fail) && !method.IsGenericMethod && method.GetParameters().Length == 0))!;

        var cells = nativeTypes.SelectMany(type => Enumerable.Range(0, cellCount)
            .Select(_ => AddLocal(body, type))).ToArray();
        var pins = Enumerable.Range(0, cellCount).Select(_ =>
            AddLocal(body, new PinnedSig(new ByRefSig(module.CorLibTypes.Byte)))).ToArray();
        var addresses = AddLocal(body, new SZArraySig(module.CorLibTypes.IntPtr));
        var rootIndex = AddLocal(body, module.CorLibTypes.Int32);
        var allocation = AddLocal(body, module.CorLibTypes.IntPtr);

        il.Add(OpCodes.Ldc_I4.ToInstruction(cells.Length));
        il.Add(OpCodes.Newarr.ToInstruction(module.CorLibTypes.IntPtr.TypeDefOrRef));
        il.Add(OpCodes.Stloc.ToInstruction(addresses));
        for (var i = 0; i < cells.Length; i++) {
            il.Add(OpCodes.Ldloc.ToInstruction(addresses));
            il.Add(OpCodes.Ldc_I4.ToInstruction(i));
            il.Add(OpCodes.Ldloca.ToInstruction(cells[i]));
            il.Add(OpCodes.Conv_I.ToInstruction());
            il.Add(OpCodes.Stelem_I.ToInstruction());
        }

        il.Add(OpCodes.Ldarg_0.ToInstruction());
        il.Add(OpCodes.Ldloc.ToInstruction(addresses));
        il.Add(OpCodes.Call.ToInstruction(attach));

        void Call(IMethod target) {
            il.Add(OpCodes.Ldarg_0.ToInstruction());
            il.Add(OpCodes.Call.ToInstruction(target));
        }

        void Roots() {
            if (pins.Length == 0)
                return;
            var next = OpCodes.Nop.ToInstruction();
            var done = OpCodes.Nop.ToInstruction();
            var targets = pins.Select(_ => OpCodes.Nop.ToInstruction()).ToArray();
            il.Add(next);
            Call(nextRoot);
            il.Add(OpCodes.Stloc.ToInstruction(rootIndex));
            il.Add(OpCodes.Ldloc.ToInstruction(rootIndex));
            il.Add(OpCodes.Switch.ToInstruction(targets));
            il.Add(OpCodes.Br.ToInstruction(done));
            for (var i = 0; i < pins.Length; i++) {
                il.Add(targets[i]);
                il.Add(OpCodes.Ldarg_0.ToInstruction());
                il.Add(OpCodes.Ldc_I4.ToInstruction(i));
                il.Add(OpCodes.Call.ToInstruction(root));
                il.Add(OpCodes.Stloc.ToInstruction(pins[i]));
                il.Add(OpCodes.Ldarg_0.ToInstruction());
                il.Add(OpCodes.Ldc_I4.ToInstruction(i));
                il.Add(OpCodes.Ldloc.ToInstruction(pins[i]));
                il.Add(OpCodes.Conv_I.ToInstruction());
                il.Add(OpCodes.Call.ToInstruction(pinRoot));
                il.Add(OpCodes.Br.ToInstruction(next));
            }

            il.Add(done);
        }

        void Driver(int depth) {
            var again = OpCodes.Nop.ToInstruction();
            var bodyStart = OpCodes.Nop.ToInstruction();
            var readyStart = OpCodes.Nop.ToInstruction();
            var filterLoop = OpCodes.Nop.ToInstruction();
            var searched = OpCodes.Nop.ToInstruction();
            var handled = OpCodes.Nop.ToInstruction();
            var filterEnd = OpCodes.Endfilter.ToInstruction();
            var resume = OpCodes.Nop.ToInstruction();
            var finallyEnd = OpCodes.Endfinally.ToInstruction();
            var done = OpCodes.Nop.ToInstruction();
            var exception = AddLocal(body, module.CorLibTypes.GetTypeRef("System", "Exception").ToTypeSig());

            il.Add(again);
            var tryStart = OpCodes.Br.ToInstruction(readyStart);
            il.Add(tryStart);
            il.Add(bodyStart);
            Call(step);
            Roots();
            il.Add(readyStart);
            Call(ready);
            il.Add(OpCodes.Brtrue.ToInstruction(bodyStart));
            il.Add(OpCodes.Leave.ToInstruction(done));

            var filterStart = OpCodes.Stloc.ToInstruction(exception);
            il.Add(filterStart);
            il.Add(OpCodes.Ldloc.ToInstruction(exception));
            il.Add(OpCodes.Isinst.ToInstruction(transferType));
            il.Add(OpCodes.Brtrue.ToInstruction(searched));
            il.Add(OpCodes.Ldarg_0.ToInstruction());
            il.Add(OpCodes.Ldloc.ToInstruction(exception));
            il.Add(OpCodes.Call.ToInstruction(beginSearch));
            il.Add(filterLoop);
            Call(searchStep);
            il.Add(OpCodes.Brfalse.ToInstruction(handled));
            Roots();
            il.Add(OpCodes.Br.ToInstruction(filterLoop));
            il.Add(handled);
            Call(hasHandler);
            il.Add(OpCodes.Br.ToInstruction(filterEnd));
            il.Add(searched);
            il.Add(OpCodes.Ldc_I4_0.ToInstruction());
            il.Add(filterEnd);

            var handlerStart = OpCodes.Pop.ToInstruction();
            il.Add(handlerStart);
            il.Add(OpCodes.Ldarg_0.ToInstruction());
            il.Add((depth == 0 ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1).ToInstruction());
            il.Add(OpCodes.Call.ToInstruction(catchMethod));
            il.Add(OpCodes.Brfalse.ToInstruction(resume));
            Call(errorMethod);
            il.Add(OpCodes.Newobj.ToInstruction(transferConstructor));
            il.Add(OpCodes.Throw.ToInstruction());
            il.Add(resume);
            il.Add(OpCodes.Leave.ToInstruction(again));

            var finallyStart = OpCodes.Ldarg_0.ToInstruction();
            il.Add(finallyStart);
            il.Add(OpCodes.Call.ToInstruction(beginEscape));
            il.Add(OpCodes.Brfalse.ToInstruction(finallyEnd));
            if (depth < unwindDepth)
                Driver(depth + 1);
            else
                il.Add(OpCodes.Call.ToInstruction(crash));
            il.Add(finallyEnd);
            il.Add(done);

            body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Filter) {
                TryStart = tryStart,
                TryEnd = filterStart,
                FilterStart = filterStart,
                HandlerStart = handlerStart,
                HandlerEnd = finallyStart,
            });
            body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally) {
                TryStart = tryStart,
                TryEnd = finallyStart,
                HandlerStart = finallyStart,
                HandlerEnd = done,
            });
        }

        Roots();
        var restart = OpCodes.Nop.ToInstruction();
        var finished = OpCodes.Nop.ToInstruction();
        il.Add(restart);
        if (hasExceptionRegions) {
            var outerTryStart = OpCodes.Nop.ToInstruction();
            il.Add(outerTryStart);
            Driver(0);
            il.Add(OpCodes.Leave.ToInstruction(finished));
            var transferHandler = OpCodes.Pop.ToInstruction();
            il.Add(transferHandler);
            Call(resumeTransfer);
            il.Add(OpCodes.Leave.ToInstruction(restart));
            il.Add(finished);
            body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch) {
                CatchType = transferType,
                TryStart = outerTryStart,
                TryEnd = transferHandler,
                HandlerStart = transferHandler,
                HandlerEnd = finished,
            });
        }
        else {
            Call(ready);
            il.Add(OpCodes.Brfalse.ToInstruction(finished));
            Call(step);
            Roots();
            il.Add(OpCodes.Br.ToInstruction(restart));
            il.Add(finished);
        }

        var returned = OpCodes.Nop.ToInstruction();
        Call(needsAllocation);
        il.Add(OpCodes.Brfalse.ToInstruction(returned));
        Call(allocationSize);
        il.Add(OpCodes.Localloc.ToInstruction());
        il.Add(OpCodes.Stloc.ToInstruction(allocation));
        il.Add(OpCodes.Ldarg_0.ToInstruction());
        il.Add(OpCodes.Ldloc.ToInstruction(allocation));
        il.Add(OpCodes.Call.ToInstruction(allocated));
        il.Add(OpCodes.Br.ToInstruction(restart));
        il.Add(returned);
        if (returnType.IsByRef) {
            var elementType = returnType.Next.RemovePinnedAndModifiers();
            if (elementType.ElementType == ElementType.TypedByRef) {
                il.Add(OpCodes.Ldarg_0.ToInstruction());
                il.Add(OpCodes.Ldfld.ToInstruction(frameResult));
                il.Add(OpCodes.Castclass.ToInstruction(referenceType));
                il.Add(OpCodes.Callvirt.ToInstruction(referenceBytes));
            }
            else {
                var storageType = elementType.ElementType is ElementType.Ptr or ElementType.FnPtr
                    ? module.CorLibTypes.IntPtr
                    : elementType;
                var result = new MethodSpecUser(referenceResult, new GenericInstMethodSig(storageType));
                Call(result);
            }
        }
        else
            Call(valueResult);

        il.Add(OpCodes.Ret.ToInstruction());
        body.OptimizeMacros();
        body.OptimizeBranches();

        /*  but god said: found your VMEnter via signature :fear:
            TODO: mutate opcodes (i think we already do, but oh well!)
         */
    }

    private static Local AddLocal(CilBody body, TypeSig type) {
        var local = new Local(type);
        body.Variables.Add(local);
        return local;
    }
}