using System.Text;
using dnlib.DotNet;
using SharpHide.Core.Engine;

namespace SharpHide.Core;

public sealed class ObfuscationContext {
    private int nameCounter;

    public ObfuscationContext(ModuleDef module, Random random) : this(module, random, null) { }

    internal ObfuscationContext(ModuleDef module, Random random, IReadOnlySet<string>? originalTypeNames) {
        Module = module;
        Random = random;
        OriginalTypes = originalTypeNames is null
            ? new HashSet<TypeDef>(module.GetTypes())
            : new HashSet<TypeDef>(module.GetTypes().Where(type => originalTypeNames.Contains(type.FullName)));
    }

    public ModuleDef Module { get; }
    public Random Random { get; }
    public ObfuscationSettings Settings { get; internal set; } = new();
    public ProtectionPipeline Pipeline { get; internal set; } = null!;
    public Action<string> Log { get; init; } = static _ => { };
    public Action<ProtectionProgress> Progress { get; init; } = static _ => { };

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Options { get; internal set; }
        = new Dictionary<string, IReadOnlyDictionary<string, double>>();

    public double OptionFor(string protection, string key) =>
        Options.TryGetValue(protection, out var bag) && bag.TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException($"protection '{protection}' has no option '{key}'");

    public HashSet<TypeDef> OriginalTypes { get; }

    public Dictionary<string, string> RenameMap { get; } = new(StringComparer.Ordinal);

    public bool RequiresIntegrityTrailer { get; set; }
    public byte[]? IntegrityKey { get; set; }

    public HashSet<TypeDef> Runtime { get; } = new();

    public HashSet<TypeDef> VirtualMachineTypes { get; } = new();

    public HashSet<Resource> RuntimeResources { get; } = new();

    public IEnumerable<MethodDef> EncryptableMethods() =>
        Module.GetTypes().Where(type => !Runtime.Contains(type))
            .SelectMany(type => type.Methods).Where(method => method.HasBody);

    public bool IsStartupCritical(MethodDef method) =>
        method == Module.EntryPoint || method.IsStaticConstructor;

    public bool Chance(double probability) => Random.NextDouble() < probability;

    public bool IsInjected(TypeDef type) => !OriginalTypes.Contains(type);

    private HashSet<string>? xamlProtectedNames;

    public HashSet<string> XamlProtectedNames => xamlProtectedNames ??= CollectResourceNames();

    private HashSet<string> CollectResourceNames() {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var resource in Module.Resources.OfType<EmbeddedResource>()) {
            var resourceName = resource.Name.String;
            if (!resourceName.EndsWith(".resources", StringComparison.Ordinal)
                && !resourceName.EndsWith(".baml", StringComparison.Ordinal))
                continue;
            ExtractIdentifiers(resource.CreateReader().ToArray(), names);
        }
        return names;
    }

    private static void ExtractIdentifiers(byte[] data, HashSet<string> names) {
        var start = -1;
        for (var i = 0; i <= data.Length; i++) {
            if (i < data.Length && IsIdentifierByte(data[i])) {
                if (start < 0)
                    start = i;
            }
            else if (start >= 0) {
                foreach (var segment in Encoding.ASCII.GetString(data, start, i - start).Split('.', '`'))
                    if (segment.Length >= 2)
                        names.Add(segment);
                start = -1;
            }
        }
    }

    private static bool IsIdentifierByte(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z'
            or >= (byte)'0' and <= (byte)'9' or (byte)'_' or (byte)'.' or (byte)'`';

    private List<(MemberRef Ref, IMemberDef Target)>? renameReferences;

    public void CaptureRenameReferences() {
        if (renameReferences is not null)
            return;
        renameReferences = new List<(MemberRef, IMemberDef)>();
        var comparer = new SigComparer();
        var seen = new HashSet<MemberRef>();
        foreach (var type in Module.GetTypes())
            foreach (var method in type.Methods) {
                if (!method.HasBody)
                    continue;
                foreach (var instruction in method.Body.Instructions) {
                    var memberRef = instruction.Operand as MemberRef
                                    ?? (instruction.Operand as MethodSpec)?.Method as MemberRef;
                    if (memberRef is null || !seen.Add(memberRef))
                        continue;
                    var declaringType = memberRef.DeclaringType?.ScopeType?.ResolveTypeDef();
                    if (declaringType is null || declaringType.Module != Module)
                        continue;
                    IMemberDef? target = memberRef.IsFieldRef
                        ? declaringType.Fields.FirstOrDefault(field => field.Name == memberRef.Name)
                        : declaringType.Methods.FirstOrDefault(candidate =>
                            candidate.Name == memberRef.Name
                            && comparer.Equals(candidate.MethodSig, memberRef.MethodSig));
                    if (target is not null)
                        renameReferences.Add((memberRef, target));
                }
            }
    }

    public void ApplyRenameReferences() {
        if (renameReferences is null)
            return;
        foreach (var (memberRef, target) in renameReferences)
            if (memberRef.Name != target.Name)
                memberRef.Name = target.Name;
    }

    private readonly HashSet<string> usedNames = new(StringComparer.Ordinal);

    public string NewName() {
        Span<char> buffer = stackalloc char[8];
        while (true) {
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = (char)('a' + Random.Next(26));
            var name = new string(buffer) + (nameCounter++).ToString("x");
            if (usedNames.Add(name))
                return name;
        }
    }

    private static readonly (int Start, int Count)[] ExoticRanges = {
        (0x05D0, 27),     // Hebrew
        (0x0905, 53),     // Devanagari
        (0x0410, 64),     // Cyrillic
        (0x30A1, 90),     // Katakana
        (0x4E00, 0x5000), // CJK
    };

    public UTF8String NewExoticName() {
        while (true) {
            var length = Random.Next(48, 73);
            var characters = new char[length];
            for (var i = 0; i < length; i++) {
                var range = ExoticRanges[Random.Next(ExoticRanges.Length)];
                characters[i] = (char)(range.Start + Random.Next(range.Count));
            }
            var name = new string(characters);
            if (usedNames.Add(name))
                return new UTF8String(name);
        }
    }

    // New runtime members live in this type and are renamed with the rest of injected code.
    public TypeDef NewContainer() {
        var type = new TypeDefUser(string.Empty, NewName(), Module.CorLibTypes.Object.TypeDefOrRef) {
            Attributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed,
        };
        Module.Types.Add(type);
        return type;
    }
}
