using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Engine.Protections;

// Adds inert attribute types and random constructor data to metadata.
public sealed class JunkAttributeProtection : Protection {
    public override long Priority => 35;
    public override string Name => "junk-attributes";
    public override string Description => "trashes methods with garbage attributes";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["rate"] = 0.4, ["types"] = 6, ["max-per-member"] = 3,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["rate"] = "Percentage of the methods which will receive garbage attributes",
        ["types"] = "Number of garbage attributes injected into the final image, between 1 and 32.",
        ["max-per-member"] =
            "Max number of garbage attributes that can appear in a single member of a type, between 1 and 8.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    private sealed record Decoy(MethodDef Ctor, TypeSig[] Params);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var rng = context.Random;
        var rate = Math.Clamp(Option(context, "rate"), 0.0, 1.0);
        var typeCount = Math.Clamp((int)Option(context, "types"), 1, 32);
        var maxPer = Math.Clamp((int)Option(context, "max-per-member"), 1, 8);

        var importer = new Importer(module);
        var attributeBase = importer.Import(typeof(Attribute));
        var attributeCtor = (IMethodDefOrRef)importer.Import(typeof(Attribute).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                                                    | System.Reflection.BindingFlags.Public, null, Type.EmptyTypes,
            null)!);

        var pool = new TypeSig[] {
            module.CorLibTypes.Int32, module.CorLibTypes.String, module.CorLibTypes.Boolean,
            module.CorLibTypes.Int64, module.CorLibTypes.Double,
        };

        var decoys = new Decoy[typeCount];
        for (var i = 0; i < typeCount; i++) {
            var type = new TypeDefUser(string.Empty, context.NewName(), attributeBase) {
                Attributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed
                             | TypeAttributes.BeforeFieldInit,
            };
            module.Types.Add(type);

            var parameters = new TypeSig[rng.Next(1, 4)];
            for (var p = 0; p < parameters.Length; p++)
                parameters[p] = pool[rng.Next(pool.Length)];

            var ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, parameters),
                    MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    MethodAttributes.Public | MethodAttributes.HideBySig
                                            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
                { Body = new CilBody() };
            ctor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            ctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(attributeCtor));
            ctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            type.Methods.Add(ctor);

            decoys[i] = new Decoy(ctor, parameters);
        }

        var decoyTypes = decoys.Select(decoy => decoy.Ctor.DeclaringType).ToHashSet();

        CustomAttribute Build() {
            var decoy = decoys[rng.Next(decoys.Length)];
            var arguments = decoy.Params.Select(type => new CAArgument(type, RandomValue(type, rng))).ToList();
            return new CustomAttribute(decoy.Ctor, arguments);
        }

        void Sprinkle(IHasCustomAttribute member) {
            if (!context.Chance(rate))
                return;

            var n = rng.Next(1, maxPer + 1);
            for (var k = 0; k < n; k++)
                member.CustomAttributes.Add(Build());
        }

        var count = 0;
        foreach (var type in module.GetTypes()) {
            if (decoyTypes.Contains(type))
                continue;

            Sprinkle(type);
            foreach (var method in type.Methods)
                Sprinkle(method);
            foreach (var field in type.Fields)
                Sprinkle(field);

            count++;
        }

        context.Log($"    {decoys.Length} decoy attributes over {count} types");
    }

    private static object RandomValue(TypeSig type, Random rng) => type.ElementType switch {
        ElementType.I4 => rng.Next(),
        ElementType.I8 => rng.NextInt64(),
        ElementType.R8 => rng.NextDouble() * 1e6,
        ElementType.Boolean => rng.Next(2) == 0,
        _ => new UTF8String(rng.Next().ToString("x")),
    };
}