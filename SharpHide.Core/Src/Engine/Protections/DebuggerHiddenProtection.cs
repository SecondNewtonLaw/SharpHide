using System.Diagnostics;
using dnlib.DotNet;

namespace SharpHide.Core.Engine.Protections;

public sealed class DebuggerHiddenProtection : Protection {
    public override long Priority => 25;
    public override string Name => "debugger-hidden";
    public override string Description => "injects junk debugger attributes to mess up debuggers real bad.";

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var rng = context.Random;
        var importer = new Importer(module);

        var methodAttributes = new[] {
            typeof(DebuggerHiddenAttribute),
            typeof(DebuggerStepThroughAttribute),
            typeof(DebuggerNonUserCodeAttribute),
        }.Select(type => (
            Name: type.FullName!,
            Ctor: (ICustomAttributeType)importer.Import(type.GetConstructor(Type.EmptyTypes)!))).ToArray();

        var browsableName = typeof(DebuggerBrowsableAttribute).FullName!;
        var browsableCtor = (ICustomAttributeType)importer.Import(
            typeof(DebuggerBrowsableAttribute).GetConstructor([typeof(DebuggerBrowsableState)])!);
        var browsableState = importer.Import(typeof(DebuggerBrowsableState)).ToTypeSig();

        var displayName = typeof(DebuggerDisplayAttribute).FullName!;
        var displayCtor = (ICustomAttributeType)importer.Import(
            typeof(DebuggerDisplayAttribute).GetConstructor([typeof(string)])!);

        var methods = 0;
        foreach (var type in module.GetTypes()) {
            if (!type.IsGlobalModuleType && !type.CustomAttributes.IsDefined(displayName))
                type.CustomAttributes.Add(new CustomAttribute(displayCtor,
                    [new CAArgument(module.CorLibTypes.String, new UTF8String(rng.Next().ToString("x")))]));

            foreach (var method in type.Methods) {
                if (!method.HasBody)
                    continue;

                foreach (var (name, ctor) in methodAttributes)
                    if (!method.CustomAttributes.IsDefined(name))
                        method.CustomAttributes.Add(new CustomAttribute(ctor));

                methods++;
            }

            foreach (var field in type.Fields)
                if (!field.CustomAttributes.IsDefined(browsableName))
                    field.CustomAttributes.Add(new CustomAttribute(browsableCtor,
                        [new CAArgument(browsableState, (int)DebuggerBrowsableState.Never)]));
        }

        AddAssemblyAttributes(context, importer);

        context.Log($"    {methods} methods tagged debugger-blind, plus field/type/assembly hints");
    }

    private static void AddAssemblyAttributes(ObfuscationContext context, Importer importer) {
        var assembly = context.Module.Assembly;
        if (assembly is null)
            return;

        var suppressIldasm = Type.GetType("System.Runtime.CompilerServices.SuppressIldasmAttribute");
        if (suppressIldasm?.GetConstructor(Type.EmptyTypes) is { } suppressCtor
            && !assembly.CustomAttributes.IsDefined(suppressIldasm.FullName!))
            assembly.CustomAttributes.Add(new CustomAttribute((ICustomAttributeType)importer.Import(suppressCtor)));

        var obfuscationName = typeof(System.Reflection.ObfuscationAttribute).FullName!;
        if (assembly.CustomAttributes.IsDefined(obfuscationName))
            return;

        var obfuscation = new CustomAttribute((ICustomAttributeType)importer.Import(
            typeof(System.Reflection.ObfuscationAttribute).GetConstructor(Type.EmptyTypes)!));
        var stringType = context.Module.CorLibTypes.String;
        var boolType = context.Module.CorLibTypes.Boolean;
        obfuscation.NamedArguments.Add(new CANamedArgument(false, stringType, "Feature",
            new CAArgument(stringType, new UTF8String("virtualization;controlflow;renaming"))));
        obfuscation.NamedArguments.Add(new CANamedArgument(false, boolType, "Exclude",
            new CAArgument(boolType, false)));
        assembly.CustomAttributes.Add(obfuscation);
    }
}
