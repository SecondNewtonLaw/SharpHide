using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4,
    DecryptHashChance = 0.7)]
internal static class NetworkProtocolCases {
    public static bool Case0851() { var address = IPAddress.Parse("192.0.2.51"); return address.GetAddressBytes().SequenceEqual(new byte[] { 192, 0, 2, 51 }); }
    public static bool Case0852() { return IPAddress.Parse("::1").Equals(IPAddress.IPv6Loopback) && IPAddress.IsLoopback(IPAddress.IPv6Loopback); }
    public static bool Case0853() { var mapped = IPAddress.Parse("203.0.113.53").MapToIPv6(); return mapped.IsIPv4MappedToIPv6 && mapped.MapToIPv4().ToString() == "203.0.113.53"; }
    public static bool Case0854() { return IPAddress.IsLoopback(IPAddress.Parse("127.8.5.4")) && !IPAddress.IsLoopback(IPAddress.Parse("192.0.2.1")); }
    public static bool Case0855() { return !IPAddress.TryParse("999.8.5.5", out _) && !IPAddress.TryParse("not-an-ip", out _); }
    public static bool Case0856() { var endpoint = new IPEndPoint(IPAddress.Loopback, 8560); return endpoint.Port == 8560 && endpoint.Address.Equals(IPAddress.Loopback); }
    public static bool Case0857() { var endpoint = new IPEndPoint(IPAddress.Parse("192.0.2.57"), 8570); var restored = (IPEndPoint)endpoint.Create(endpoint.Serialize()); return restored.Equals(endpoint); }
    public static bool Case0858() { var address = new SocketAddress(AddressFamily.InterNetwork, 16); address[2] = 0x21; address[3] = 0x8a; return address[2] == 0x21 && address[3] == 0x8a && address.Size == 16; }
    public static bool Case0859() { var prefix = 24; var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix); return mask == 0xffffff00u && BitOperations.PopCount(mask) == 24; }
    public static bool Case0860() { var address = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse("192.168.10.60").GetAddressBytes()); var network = BinaryPrimitives.ReadUInt32BigEndian(IPAddress.Parse("192.168.10.0").GetAddressBytes()); const uint mask = 0xffffff00; return (address & mask) == (network & mask); }
    public static bool Case0861() { var address = IPAddress.Parse("fe80::861%3"); return address.AddressFamily == AddressFamily.InterNetworkV6 && address.ScopeId == 3; }
    public static bool Case0862() { var relative = new Uri("api/items/862?active=true", UriKind.Relative); return !relative.IsAbsoluteUri && relative.OriginalString.StartsWith("api/", StringComparison.Ordinal); }
    public static bool Case0863() { var uri = new Uri(new Uri("https://example.test/root/"), "../api/863"); return uri.AbsoluteUri == "https://example.test/api/863"; }
    public static bool Case0864() { var escaped = Uri.EscapeDataString("case 864/λ"); return !escaped.Contains(' ') && Uri.UnescapeDataString(escaped) == "case 864/λ"; }
    public static bool Case0865() { return Uri.UnescapeDataString("case%20865%2Fok") == "case 865/ok"; }
    public static bool Case0866() { var mapping = new IdnMapping(); var ascii = mapping.GetAscii("münich.example"); return ascii.StartsWith("xn--", StringComparison.Ordinal) && mapping.GetUnicode(ascii) == "münich.example"; }
    public static bool Case0867() { return Uri.CheckHostName("example.test") == UriHostNameType.Dns && Uri.CheckHostName("192.0.2.67") == UriHostNameType.IPv4; }
    public static bool Case0868() { var builder = new UriBuilder { Scheme = "https", Host = "example.test", Port = 8680, Path = "v1/items" }; return builder.Uri.Port == 8680 && builder.Uri.AbsolutePath == "/v1/items"; }
    public static bool Case0869() { var uri = new Uri("https://example.test/?id=869&name=case"); var parts = uri.Query.TrimStart('?').Split('&').Select(item => item.Split('=', 2)).ToDictionary(item => item[0], item => item[1]); return parts["id"] == "869" && parts["name"] == "case"; }
    public static bool Case0870() { var uri = new Uri("https://example.test/path#section-870"); return uri.Fragment == "#section-870" && uri.GetLeftPart(UriPartial.Path) == "https://example.test/path"; }
    public static bool Case0871() { var addresses = Dns.GetHostAddresses("localhost"); return addresses.Length > 0 && addresses.Any(IPAddress.IsLoopback); }
    public static bool Case0872() { var entry = Dns.GetHostEntry("localhost"); return entry.AddressList.Any(IPAddress.IsLoopback) && !string.IsNullOrWhiteSpace(entry.HostName); }
    public static bool Case0873() { var addresses = Dns.GetHostAddressesAsync("localhost").GetAwaiter().GetResult(); return addresses.Any(IPAddress.IsLoopback); }
    public static bool Case0874() { return IPAddress.Any.Equals(IPAddress.Parse("0.0.0.0")) && IPAddress.None.Equals(IPAddress.Parse("255.255.255.255")); }
    public static bool Case0875() { return IPAddress.IPv6Any.Equals(IPAddress.Parse("::")) && IPAddress.IPv6None.Equals(IPAddress.Parse("::")); }
    public static bool Case0876() { return Socket.OSSupportsIPv4 || Socket.OSSupportsIPv6; }
    public static bool Case0877() { return ProtocolType.Tcp != ProtocolType.Udp && (int)ProtocolType.Tcp == 6 && (int)ProtocolType.Udp == 17; }
    public static bool Case0878() { return IPAddress.Loopback.AddressFamily == AddressFamily.InterNetwork && IPAddress.IPv6Loopback.AddressFamily == AddressFamily.InterNetworkV6; }
    public static bool Case0879() { using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); return socket.SocketType == SocketType.Stream && socket.ProtocolType == ProtocolType.Tcp; }
    public static bool Case0880() { var first = new Uri("https://example.test:443/path"); var second = new Uri("https://example.test/path"); return first == second && first.IsDefaultPort; }
    public static bool Case0881() { return TcpExchange("tcp-881"u8.ToArray(), bytes => bytes.Reverse().ToArray()).SequenceEqual("188-pct"u8.ToArray()); }
    public static bool Case0882() { return Encoding.UTF8.GetString(TcpExchange("lower-882"u8.ToArray(), bytes => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).ToUpperInvariant()))) == "LOWER-882"; }
    public static bool Case0883() { return TcpExchange(new byte[] { 8, 8, 3 }, bytes => bytes.Select(value => (byte)(value + 1)).ToArray()).SequenceEqual(new byte[] { 9, 9, 4 }); }
    public static bool Case0884() { var request = Enumerable.Range(0, 884).Select(value => (byte)value).ToArray(); return TcpExchange(request, SHA256.HashData).SequenceEqual(SHA256.HashData(request)); }
    public static bool Case0885() { Span<byte> payload = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(payload, 885); var response = TcpExchange(payload.ToArray(), bytes => bytes); return BinaryPrimitives.ReadInt32BigEndian(response) == 885; }
    public static bool Case0886() { return TcpExchange([], bytes => Encoding.ASCII.GetBytes("empty-886")).SequenceEqual("empty-886"u8.ToArray()); }
    public static bool Case0887() { var response = TcpExchange("split-887"u8.ToArray(), bytes => bytes[..5].Concat(bytes[6..]).ToArray()); return Encoding.UTF8.GetString(response) == "split887"; }
    public static bool Case0888() { var response = TcpExchange(new byte[] { 8, 8, 8 }, bytes => [.. bytes, (byte)8]); return response.SequenceEqual(new byte[] { 8, 8, 8, 8 }); }
    public static bool Case0889() { var response = TcpExchange("889"u8.ToArray(), bytes => Encoding.ASCII.GetBytes(Convert.ToHexString(bytes))); return Encoding.ASCII.GetString(response) == "383839"; }
    public static bool Case0890() { var response = TcpExchange("case-890"u8.ToArray(), bytes => Encoding.UTF8.GetBytes(Convert.ToBase64String(bytes))); return Encoding.UTF8.GetString(response) == Convert.ToBase64String("case-890"u8); }
    public static bool Case0891() { return UdpExchange("udp-891"u8.ToArray(), bytes => bytes.Reverse().ToArray()).SequenceEqual("198-pdu"u8.ToArray()); }
    public static bool Case0892() { return Encoding.UTF8.GetString(UdpExchange("lower-892"u8.ToArray(), bytes => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).ToUpperInvariant()))) == "LOWER-892"; }
    public static bool Case0893() { return UdpExchange(new byte[] { 8, 9, 3 }, bytes => bytes.Select(value => (byte)(value ^ 0xff)).ToArray()).SequenceEqual(new byte[] { 247, 246, 252 }); }
    public static bool Case0894() { var request = Enumerable.Range(0, 894).Select(value => (byte)value).ToArray(); return UdpExchange(request, SHA256.HashData).SequenceEqual(SHA256.HashData(request)); }
    public static bool Case0895() { Span<byte> payload = stackalloc byte[8]; BinaryPrimitives.WriteInt64LittleEndian(payload, 895); return BinaryPrimitives.ReadInt64LittleEndian(UdpExchange(payload.ToArray(), bytes => bytes)) == 895; }
    public static bool Case0896() { var response = UdpExchange("896"u8.ToArray(), bytes => [.. bytes, (byte)'!']); return Encoding.UTF8.GetString(response) == "896!"; }
    public static bool Case0897() { var response = UdpExchange("prefix-897"u8.ToArray(), bytes => bytes[7..]); return Encoding.UTF8.GetString(response) == "897"; }
    public static bool Case0898() { var response = UdpExchange(new byte[] { 8, 9, 8 }, bytes => bytes.Order().ToArray()); return response.SequenceEqual(new byte[] { 8, 8, 9 }); }
    public static bool Case0899() { var response = UdpExchange("899"u8.ToArray(), bytes => Encoding.ASCII.GetBytes(Convert.ToHexString(bytes))); return Encoding.ASCII.GetString(response) == "383939"; }
    public static bool Case0900() { var response = UdpExchange("case-900"u8.ToArray(), bytes => Encoding.UTF8.GetBytes(Convert.ToBase64String(bytes))); return Encoding.UTF8.GetString(response) == Convert.ToBase64String("case-900"u8); }

    private static byte[] TcpExchange(byte[] request, Func<byte[], byte[]> transform) {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var accepting = Task.Run(listener.AcceptTcpClient);
        using var client = new TcpClient { ReceiveTimeout = 2000, SendTimeout = 2000 };
        client.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
        if (!accepting.Wait(2000))
            return [];
        using var server = accepting.Result;
        server.ReceiveTimeout = 2000;
        server.SendTimeout = 2000;
        var clientStream = client.GetStream();
        clientStream.Write(request);
        client.Client.Shutdown(SocketShutdown.Send);
        var serverStream = server.GetStream();
        using var incoming = new MemoryStream();
        serverStream.CopyTo(incoming);
        var response = transform(incoming.ToArray());
        serverStream.Write(response);
        server.Client.Shutdown(SocketShutdown.Send);
        using var output = new MemoryStream();
        clientStream.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] UdpExchange(byte[] request, Func<byte[], byte[]> transform) {
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        using var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        server.Client.ReceiveTimeout = 2000;
        client.Client.ReceiveTimeout = 2000;
        var endpoint = (IPEndPoint)server.Client.LocalEndPoint!;
        client.Send(request, request.Length, endpoint);
        IPEndPoint remote = new(IPAddress.Any, 0);
        var incoming = server.Receive(ref remote);
        var response = transform(incoming);
        server.Send(response, response.Length, remote);
        return client.Receive(ref remote);
    }
}
