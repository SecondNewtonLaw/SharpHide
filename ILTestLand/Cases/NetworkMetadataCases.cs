using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class NetworkMetadataCases
{
    public static bool Case1901() { var value = NetworkInterface.GetAllNetworkInterfaces(); return value.Length > 0; }
    public static bool Case1902() { var value = NetworkInterface.GetAllNetworkInterfaces().Select(item => item.NetworkInterfaceType); return value.Any(); }
    public static bool Case1903() { var value = NetworkInterface.GetAllNetworkInterfaces().Select(item => item.OperationalStatus); return value.Any(); }
    public static bool Case1904() { var value = NetworkInterface.GetAllNetworkInterfaces().Select(item => item.Description); return value.All(text => text != null); }
    public static bool Case1905() { var value = NetworkInterface.GetAllNetworkInterfaces().Select(item => item.Id); return value.All(text => !string.IsNullOrEmpty(text)); }
    public static bool Case1906() { var value = NetworkInterface.GetAllNetworkInterfaces().Select(item => item.GetPhysicalAddress()); return value.All(address => address != null); }
    public static bool Case1907() { var value = NetworkInterface.LoopbackInterfaceIndex; return value >= 0; }
    public static bool Case1908() { var value = IPGlobalProperties.GetIPGlobalProperties(); return value != null; }
    public static bool Case1909() { var value = IPGlobalProperties.GetIPGlobalProperties().DomainName; return value != null; }
    public static bool Case1910() { var value = IPGlobalProperties.GetIPGlobalProperties().HostName; return !string.IsNullOrEmpty(value); }
    public static bool Case1911() { var value = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections(); return value != null; }
    public static bool Case1912() { var value = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners(); return value != null; }
    public static bool Case1913() { var value = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners(); return value != null; }
    public static bool Case1914() { var value = IPGlobalProperties.GetIPGlobalProperties().GetTcpIPv4Statistics(); return value.CurrentConnections >= 0; }
    public static bool Case1915() { var value = IPGlobalProperties.GetIPGlobalProperties().GetUdpIPv4Statistics(); return value.DatagramsReceived >= 0; }
    public static bool Case1916() { var value = new IPAddress([127, 0, 0, 1]); return IPAddress.IsLoopback(value); }
    public static bool Case1917() { var value = new IPAddress([192, 168, 1, 17]); return value.GetAddressBytes()[3] == 17; }
    public static bool Case1918() { var value = IPAddress.Parse("::ffff:192.0.2.1"); return value.IsIPv4MappedToIPv6; }
    public static bool Case1919() { var value = IPAddress.Parse("::ffff:192.0.2.1").MapToIPv4(); return value.Equals(IPAddress.Parse("192.0.2.1")); }
    public static bool Case1920() { var value = IPAddress.Parse("192.0.2.1").MapToIPv6(); return value.IsIPv4MappedToIPv6; }
    public static bool Case1921() { var value = IPAddress.Parse("192.0.2.1").GetAddressBytes(); return value.Length == 4; }
    public static bool Case1922() { var value = IPAddress.Parse("2001:db8::1").GetAddressBytes(); return value.Length == 16; }
    public static bool Case1923() { var value = new IPEndPoint(IPAddress.Loopback, 1923).Serialize(); return value.Size > 0; }
    public static bool Case1924() { var value = new DnsEndPoint("example.test", 1924, AddressFamily.InterNetwork); return value.AddressFamily == AddressFamily.InterNetwork; }
    public static bool Case1925() { var value = new DnsEndPoint("example.test", 1925, AddressFamily.Unspecified); return value.Host == "example.test"; }
    public static bool Case1926() { var value = new UnixDomainSocketEndPoint("/tmp/1926").Serialize(); return value.Size > 0; }
    public static bool Case1927() { var value = new SocketAddress(AddressFamily.InterNetworkV6, 28); return value.Size == 28; }
    public static bool Case1928() { var value = new SocketAddress(AddressFamily.Unix); return value.Family == AddressFamily.Unix; }
    public static bool Case1929() { var value = new LingerOption(true, 1929); return value.Enabled && value.LingerTime == 1929; }
    public static bool Case1930() { var value = default(IPPacketInformation); return value.Address == null && value.Interface == 0; }
    public static bool Case1931() { return typeof(NetworkStream).IsSubclassOf(typeof(Stream)); }
    public static bool Case1932() { using var value = new TcpClient(); return !value.Connected; }
    public static bool Case1933() { using var value = new UdpClient(); return value.Client.AddressFamily == AddressFamily.InterNetwork; }
    public static bool Case1934() { using var value = new TcpListener(IPAddress.Loopback, 0); value.Start(); var endpoint = (IPEndPoint)value.LocalEndpoint; value.Stop(); return endpoint.Port > 0; }
    public static bool Case1935() { using var value = new UdpClient(0); var endpoint = (IPEndPoint)value.Client.LocalEndPoint!; return endpoint.Port > 0; }
    public static bool Case1936() { using var value = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); value.Bind(new IPEndPoint(IPAddress.Loopback, 0)); return value.LocalEndPoint is IPEndPoint; }
    public static bool Case1937() { using var value = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); value.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); return value.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive) is int enabled && enabled != 0; }
    public static bool Case1938() { using var value = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); value.ReceiveTimeout = 1938; return value.ReceiveTimeout == 1938; }
    public static bool Case1939() { using var value = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); value.SendTimeout = 1939; return value.SendTimeout == 1939; }
    public static bool Case1940() { using var value = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); value.NoDelay = true; return value.NoDelay; }
    public static bool Case1941() { using var value = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); value.Blocking = false; return !value.Blocking; }
    public static bool Case1942() { using var value = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp); value.DualMode = false; return !value.DualMode; }
    public static bool Case1943() { using var value = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp); value.DualMode = true; return value.DualMode; }
    public static bool Case1944() { using var value = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); return value.ProtocolType == ProtocolType.Tcp; }
    public static bool Case1945() { using var value = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); return value.SocketType == SocketType.Dgram; }
    public static bool Case1946() { var value = new SocketInformation(); return value.ProtocolInformation == null; }
    public static bool Case1947() { var value = new NetworkCredential("1947", "x"); return value.UserName == "1947"; }
    public static bool Case1948() { var value = new Uri("https://example.test:1948"); return value.Port == 1948; }
    public static bool Case1949() { var value = Uri.CheckHostName("example.test"); return value == UriHostNameType.Dns; }
    public static bool Case1950() { var value = Uri.TryCreate("https://example.test/1950", UriKind.Absolute, out var uri); return value && uri!.IsAbsoluteUri; }
}
