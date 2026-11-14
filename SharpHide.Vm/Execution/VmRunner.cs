using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace SharpHide.Vm;

public static class VmRunner {
    private const int MaxPayloadSize = 16_777_216 + 46;
    private const int MaxEncodedPayloadSize = ((MaxPayloadSize + 2) / 3) * 4;
    private static readonly byte[] Key = {
        0x2B, 0x7E, 0x15, 0x16, 0x28, 0xAE, 0xD2, 0xA6, 0xAB, 0xF7, 0x15, 0x88, 0x09, 0xCF, 0x4F, 0x3C,
    };

    public static object? Run(string program, object?[] args, object?[] metadata) {
        try {
            using var pins = new VmPins();
            var frame = CreateFrame(program, args, metadata, pins, [typeof(TypedReference)],
                Enumerable.Repeat(-1, metadata.Length).ToArray(), 0, 0, 0, 0);
            if (frame.ReturnType.IsByRef || frame.ReturnType.IsByRefLike || frame.CellCount != 0 || frame.UnwindDepth != 0)
                VmCrash.Fail();
            while (frame.Ready()) {
                frame.Step();
                if (frame.NeedsAllocation())
                    VmCrash.Fail();
            }
            return frame.ValueResult();
        }
        catch (BadImageFormatException) {
            VmCrash.ConsumeIntegrityFault();
            throw;
        }
    }

    public static VmFrame CreateFrame(string program, object?[] args, object?[] metadata, VmPins pins, Type[] nativeTypes,
        int[] nativeBanks, long scrambleKey, long key0, long key1, int rounds, long binding = 0,
        VmReference.Scope? scope = null) {
        scope ??= VmReference.CreateScope();
        try {
            return CreateFrameCore(program, args, metadata, pins, nativeTypes, nativeBanks, scrambleKey, key0,
                key1, rounds, binding, scope);
        }
        catch (BadImageFormatException) {
            VmCrash.ConsumeIntegrityFault();
            throw;
        }
        catch (Exception) {
            return VmCrash.Fail<VmFrame>(28);
        }
    }

    private static VmFrame CreateFrameCore(string program, object?[] args, object?[] metadata, VmPins pins,
        Type[] nativeTypes, int[] nativeBanks, long scrambleKey, long key0, long key1, int rounds, long binding,
        VmReference.Scope scope) {
        var encrypted = DecodePayload(program, key0, key1, binding);
        Transform(encrypted, unchecked((ulong)key0), unchecked((ulong)key1), rounds);
        var code = scrambleKey == 0 ? Scramble(encrypted) : Scramble(encrypted, scrambleKey);
        if (code.Length <= VmOpcodes.HeaderSize)
            VmCrash.Fail(1);
        var ip = 256;
        if (ReadInt(code, ref ip) != VmOpcodes.Magic)
            VmCrash.Fail(2);
        var registerCount = ReadInt(code, ref ip);
        var localCount = ReadInt(code, ref ip);
        var argCount = ReadInt(code, ref ip);
        var regionCount = ReadInt(code, ref ip);
        if (registerCount < 1 || registerCount > VmOpcodes.MaxFrameSize ||
            (uint)localCount > VmOpcodes.MaxFrameSize || (uint)argCount > VmOpcodes.MaxFrameSize ||
            args.Length != argCount || metadata.Length < 1 + argCount + localCount ||
            nativeBanks.Length < 1 + argCount + localCount)
            VmCrash.Fail(3);
        for (var i = 0; i < 1 + argCount + localCount; i++)
            if (metadata[i] is not Type type || type.ContainsGenericParameters ||
                i != 0 && type == typeof(void))
                VmCrash.Fail(4);
        if ((uint)regionCount > (uint)((code.Length - ip) / 28))
            VmCrash.Fail(5);
        var bodyStart = VmOpcodes.HeaderSize + regionCount * 28;
        var regions = new VmExceptionRegion[regionCount];
        for (var i = 0; i < regionCount; i++) {
            var region = new VmExceptionRegion {
                TryStart = bodyStart + ReadInt(code, ref ip),
                TryEnd = bodyStart + ReadInt(code, ref ip),
                HandlerStart = bodyStart + ReadInt(code, ref ip),
                HandlerEnd = bodyStart + ReadInt(code, ref ip),
                FilterStart = bodyStart + ReadInt(code, ref ip),
                Kind = ReadInt(code, ref ip),
            };
            var index = ReadInt(code, ref ip);
            if (region.Kind is not (0 or 1 or 2 or 4) ||
                region.Kind == 0 && ((uint)index >= (uint)metadata.Length || metadata[index] is not Type))
                VmCrash.Fail(6);
            region.CatchType = region.Kind == 0 ? (Type)metadata[index]! : null;
            regions[i] = region;
        }
        var boundaries = new bool[code.Length];
        var targets = new List<int>();
        var referenceCalls = metadata.Take(1 + argCount + localCount).Cast<Type>()
            .Any(type => type.IsByRef || type.IsByRefLike);
        while (ip < code.Length) {
            boundaries[ip] = true;
            var op = code[code[ip++]];
            var register = ReadInt(code, ref ip);
            var required = op switch {
                VmOpcodes.Br or VmOpcodes.Leave or VmOpcodes.Endfinally or VmOpcodes.Rethrow => 0,
                VmOpcodes.Ret when metadata[0] is Type returnType && returnType == typeof(void) => 0,
                VmOpcodes.Negate => 1,
                VmOpcodes.Dup or >= VmOpcodes.BitAnd and < VmOpcodes.BitNot or
                    >= VmOpcodes.Add and <= VmOpcodes.RemainderUnsigned => 2,
                _ => 1,
            };
            if ((uint)register > (uint)registerCount || required > registerCount - register)
                VmCrash.Fail(7);
            switch (op) {
                case VmOpcodes.Ldarg or VmOpcodes.Starg or VmOpcodes.Ldarga:
                    if ((uint)ReadInt(code, ref ip) >= (uint)argCount)
                        VmCrash.Fail(9);
                    break;
                case VmOpcodes.Ldloc or VmOpcodes.Stloc or VmOpcodes.Ldloca:
                    if ((uint)ReadInt(code, ref ip) >= (uint)localCount)
                        VmCrash.Fail(10);
                    break;
                case VmOpcodes.Ldc:
                    ReadInt(code, ref ip);
                    break;
                case >= VmOpcodes.BitAnd and <= VmOpcodes.BitNot or >= VmOpcodes.Add and <= VmOpcodes.RemainderUnsigned:
                    break;
                case VmOpcodes.Operation: {
                    var index = ReadInt(code, ref ip);
                    var inputCount = ReadInt(code, ref ip);
                    if ((uint)inputCount > (uint)(registerCount - register))
                        VmCrash.Fail(11);
                    if ((uint)index >= (uint)metadata.Length || metadata[index] is not VmOperation)
                        VmCrash.Fail(12);
                    var operation = (VmOperation)metadata[index]!;
                    if (operation.ResultBank < -1 || operation.ResultBank >= nativeTypes.Length ||
                        operation.ResultType.IsByRef && operation.ResultBank != 0 ||
                        operation.ResultBank > 0 && nativeTypes[operation.ResultBank] != VmValue.StorageType(operation.ResultType))
                        VmCrash.Fail(16);
                    referenceCalls |= operation.ResultType.IsByRef;
                    break;
                }
                case VmOpcodes.Localloc:
                    break;
                case VmOpcodes.Br or VmOpcodes.BrTrue or VmOpcodes.BrFalse or VmOpcodes.Leave:
                    targets.Add(ReadInt(code, ref ip));
                    break;
                case VmOpcodes.Switch: {
                    var count = ReadInt(code, ref ip);
                    if ((uint)count > (uint)((code.Length - ip) / 4))
                        VmCrash.Fail(14);
                    for (var i = 0; i < count; i++)
                        targets.Add(ReadInt(code, ref ip));
                    break;
                }
                case VmOpcodes.Dup or VmOpcodes.Pop or VmOpcodes.Ret or VmOpcodes.Endfinally or VmOpcodes.Endfilter or VmOpcodes.Rethrow:
                    break;
                default:
                    VmCrash.Fail(15);
                    break;
            }
        }
        foreach (var target in targets)
            if ((uint)target >= (uint)(code.Length - bodyStart) || !boundaries[bodyStart + target])
                VmCrash.Fail(17);
        foreach (var region in regions) {
            foreach (var start in new[] { region.TryStart, region.HandlerStart })
                if (start < bodyStart || start >= code.Length || !boundaries[start])
                    VmCrash.Fail(18);
            foreach (var end in new[] { region.TryEnd, region.HandlerEnd })
                if (end < bodyStart || end > code.Length || end != code.Length && !boundaries[end])
                    VmCrash.Fail(19);
            if (region.TryStart >= region.TryEnd || region.HandlerStart >= region.HandlerEnd ||
                region.Kind == 1 && (region.FilterStart < bodyStart || region.FilterStart >= region.HandlerStart || !boundaries[region.FilterStart]))
                VmCrash.Fail(20);
        }
        regions = regions.OrderBy(r => r.TryEnd - r.TryStart).ToArray();
        if (nativeTypes.Length == 0 || nativeTypes[0] != typeof(TypedReference) ||
            nativeTypes.Any(type => type.ContainsGenericParameters) ||
            nativeBanks.Take(1 + argCount + localCount).Any(bank => bank < -1 || bank >= nativeTypes.Length))
            VmCrash.Fail(21);
        for (var index = 0; index < 1 + argCount + localCount; index++) {
            var bank = nativeBanks[index];
            if (bank > 0 && nativeTypes[bank] != VmValue.StorageType((Type)metadata[index]!))
                VmCrash.Fail(22);
        }
        var locals = new VmReference[localCount];
        var arguments = new VmReference[argCount];
        for (var i = 0; i < argCount; i++) {
            var type = (Type)metadata[1 + i]!;
            var bank = nativeBanks[1 + i];
            if (bank >= 0) {
                var native = args[i] as VmNativeValue;
                if (native is null || native.Type != type || native.Bank != bank)
                    VmCrash.Fail(24);
                arguments[i] = VmReference.Pointer(scope, native.Address, type, bank);
            }
            else
                arguments[i] = VmReference.Slot(scope, type, args[i]);
        }
        for (var i = 0; i < localCount; i++) {
            var type = (Type)metadata[1 + argCount + i]!;
            if (nativeBanks[1 + argCount + i] >= 0 || type.IsByRefLike)
                continue;
            locals[i] = VmReference.Slot(scope, type,
                type.IsValueType && Nullable.GetUnderlyingType(type) is null ? RuntimeHelpers.GetUninitializedObject(type) : null);
        }
        return new VmFrame(code, metadata, boundaries, arguments, locals, regions, pins, bodyStart, registerCount,
            referenceCalls, nativeTypes, nativeBanks, scope);
    }

    private static byte[] DecodePayload(string program, long key0, long key1, long binding) {
        if (program is null || program.Length > MaxEncodedPayloadSize)
            VmCrash.Fail(29);
        byte[] payload;
        try {
            payload = Convert.FromBase64String(program);
        }
        catch (FormatException) {
            VmCrash.Fail(26);
            payload = [];
        }
        catch (ArgumentNullException) {
            VmCrash.Fail(26);
            payload = [];
        }
        if (payload.Length > MaxPayloadSize)
            VmCrash.Fail(29);
        var aesVersioned = payload.Length >= 42 && payload[^42..^38].SequenceEqual("SHB1"u8) &&
            payload[^38] == 1 && payload[^37] == 2;
        var versioned = !aesVersioned && payload.Length >= 46 && payload[^46..^42].SequenceEqual("SHB1"u8);
        var authenticated = versioned || payload.Length > 36 && payload[^36..^32].SequenceEqual("SHAT"u8);
        var encrypted = aesVersioned ? payload[..^42] : versioned ? payload[..^46] : authenticated ? payload[..^36] : payload;
        if (aesVersioned) {
            var header = payload[^42..^16];
            if (BitConverter.ToInt64(header, 18) != binding)
                VmCrash.Fail(25);
            var nonce = header[6..18];
            var ciphertext = encrypted.ToArray();
            try {
                using var aes = new AesGcm(EncryptionKey(key0, key1, binding, header), 16);
                aes.Decrypt(nonce, ciphertext, payload[^16..], encrypted, header);
            }
            catch (CryptographicException) {
                VmCrash.Fail(25);
            }
        }
        else if (versioned) {
            if (payload[^42] != 1 || payload[^41] != 1 || BitConverter.ToInt64(payload, payload.Length - 40) != binding)
                VmCrash.Fail(25);
            if (!CryptographicOperations.FixedTimeEquals(payload[^32..],
                    AuthenticationTag(encrypted, key0, key1, binding, payload[^46..^32].ToArray())))
                VmCrash.Fail(25);
        }
        else if (authenticated && !CryptographicOperations.FixedTimeEquals(payload[^32..], AuthenticationTag(encrypted, key0, key1, binding)))
            VmCrash.Fail(25);
        return encrypted;
    }

    public static byte[] Scramble(byte[] program) {
        var copy = (byte[])program.Clone();
        for (var i = 0; i < copy.Length; i++)
            copy[i] ^= Key[i % Key.Length];
        return copy;
    }

    public static byte[] Scramble(byte[] program, long key) {
        var copy = (byte[])program.Clone();
        var state = unchecked((ulong)key);
        ulong stream = 0;
        for (var i = 0; i < copy.Length; i++) {
            if ((i & 7) == 0) {
                state += 0x9E3779B97F4A7C15UL;
                stream = state;
                stream = (stream ^ stream >> 30) * 0xBF58476D1CE4E5B9UL;
                stream = (stream ^ stream >> 27) * 0x94D049BB133111EBUL;
                stream ^= stream >> 31;
            }
            copy[i] ^= (byte)(stream >> ((i & 7) * 8));
        }
        return copy;
    }

    public static byte[] Encrypt(byte[] program, long key0, long key1, int rounds) {
        var copy = (byte[])program.Clone();
        Transform(copy, unchecked((ulong)key0), unchecked((ulong)key1), rounds);
        return copy;
    }

    public static string Seal(byte[] encrypted, long key0, long key1, long binding = 0) =>
        Convert.ToBase64String(encrypted.Concat("SHAT"u8.ToArray()).Concat(AuthenticationTag(encrypted, key0, key1, binding)).ToArray());

    public static string SealVersioned(byte[] encrypted, long key0, long key1, long binding) {
        var header = new byte[14];
        "SHB1"u8.CopyTo(header);
        header[4] = 1;
        header[5] = 1;
        BitConverter.TryWriteBytes(header.AsSpan(6), binding);
        return Convert.ToBase64String(encrypted.Concat(header).Concat(
            AuthenticationTag(encrypted, key0, key1, binding, header)).ToArray());
    }

    public static string SealAesVersioned(byte[] encrypted, long key0, long key1, long binding) {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var header = new byte[26];
        "SHB1"u8.CopyTo(header);
        header[4] = 1;
        header[5] = 2;
        nonce.CopyTo(header.AsSpan(6));
        BitConverter.TryWriteBytes(header.AsSpan(18), binding);
        var ciphertext = new byte[encrypted.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(EncryptionKey(key0, key1, binding, header), 16))
            aes.Encrypt(nonce, encrypted, ciphertext, tag, header);
        return Convert.ToBase64String(ciphertext.Concat(header).Concat(tag).ToArray());
    }

    private static byte[] AuthenticationTag(byte[] encrypted, long key0, long key1, long binding,
        byte[]? context = null) {
        Span<byte> key = stackalloc byte[24];
        BitConverter.TryWriteBytes(key, key0);
        BitConverter.TryWriteBytes(key[8..], key1);
        BitConverter.TryWriteBytes(key[16..], binding);
        if (context is null)
            return HMACSHA256.HashData(key, encrypted);
        return HMACSHA256.HashData(key, context.Concat(encrypted).ToArray());
    }

    private static byte[] EncryptionKey(long key0, long key1, long binding, byte[] header) {
        Span<byte> material = stackalloc byte[24];
        BitConverter.TryWriteBytes(material, key0);
        BitConverter.TryWriteBytes(material[8..], key1);
        BitConverter.TryWriteBytes(material[16..], binding);
        return HMACSHA256.HashData(material, header);
    }

    private static void Transform(byte[] data, ulong key0, ulong key1, int rounds) {
        if ((uint)rounds > 64)
            VmCrash.Fail(23);
        if (rounds == 0)
            return;
        var keys = new[] { (uint)key0, (uint)(key0 >> 32), (uint)key1, (uint)(key1 >> 32) };
        const uint delta = 0x9E3779B9;
        for (var offset = 0; offset < data.Length; offset += 8) {
            var block = (ulong)(offset / 8);
            var left = (uint)block;
            var right = (uint)(block >> 32) ^ (uint)data.Length;
            uint sum = 0;
            for (var round = 0; round < rounds; round++) {
                left += ((right << 4 ^ right >> 5) + right) ^ (sum + keys[sum & 3]);
                sum += delta;
                right += ((left << 4 ^ left >> 5) + left) ^ (sum + keys[sum >> 11 & 3]);
            }
            var stream = left | (ulong)right << 32;
            var count = Math.Min(8, data.Length - offset);
            for (var index = 0; index < count; index++)
                data[offset + index] ^= (byte)(stream >> (index * 8));
        }
    }

    private static int ReadInt(byte[] code, ref int ip) {
        if (ip < 0 || ip > code.Length - 4)
            VmCrash.Fail(22);
        var value = code[ip] | (code[ip + 1] << 8) | (code[ip + 2] << 16) | (code[ip + 3] << 24);
        ip += 4;
        return value;
    }
}
