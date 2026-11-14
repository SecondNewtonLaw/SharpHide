namespace SharpHide.Core.Engine;

public abstract class Protection {
    public abstract long Priority { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }

    public virtual IReadOnlyDictionary<string, double> DefaultOptions => EmptyOptions;

    private static readonly IReadOnlyDictionary<string, double> EmptyOptions = new Dictionary<string, double>();

    public virtual IReadOnlyDictionary<string, string> OptionHelp => EmptyHelp;

    private static readonly IReadOnlyDictionary<string, string> EmptyHelp = new Dictionary<string, string>();

    protected double Option(ObfuscationContext context, string key) => context.OptionFor(Name, key);

    public abstract void Populate(ProtectionPipeline pipeline);
    public abstract void Initialize(ObfuscationContext context);

    public static IEnumerable<Protection> Discover() =>
        typeof(Protection).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Protection)))
            .Select(type => (Protection)Activator.CreateInstance(type)!);
}
