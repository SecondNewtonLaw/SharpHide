using System.Text;
using dnlib.DotNet;
using SharpHide.Core.Injection;

namespace SharpHide.Core.Payloads;

public static class StringCipherRoller {
    public sealed record Recipe(IReadOnlyList<CipherPasses.Pass> Passes);

    public static Recipe Roll(Random rng, int minPasses = 8, int maxPasses = 14, bool includeJunk = false) {
        var count = rng.Next(minPasses, maxPasses + 1);
        var passes = new List<CipherPasses.Pass>(count * 2);
        for (var i = 0; i < count; i++) {
            passes.Add(CipherPasses.Pool[rng.Next(CipherPasses.Pool.Count)](rng));
            if (includeJunk)
                passes.Add(CipherPasses.Junk);
        }
        return new Recipe(passes);
    }

    public static string Encrypt(string text, Recipe recipe) {
        var buffer = Encoding.UTF8.GetBytes(text);
        foreach (var pass in recipe.Passes)
            pass.Forward(buffer);
        return Convert.ToBase64String(buffer);
    }

    public static MethodDef EmitDecrypt(ModuleDef target, Recipe recipe, TypeDef? into = null) {
        var context = InjectionExtensions.ContextFor(target);
        var inverse = new List<(MethodDef Method, int? Seed)>(recipe.Passes.Count);
        for (var i = recipe.Passes.Count - 1; i >= 0; i--) {
            var pass = recipe.Passes[i];
            inverse.Add((InjectionExtensions.ResolveMethod(pass.Inverse), pass.Seed));
        }
        return context.BuildStringDecryptor(into ?? target.GlobalType, inverse);
    }
}
