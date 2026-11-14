using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class ProtocolInteropCases
{
    public static bool Case1601() { var value = new IPEndPoint(IPAddress.Loopback, 1601); return value.Port == 1601 && value.Address.Equals(IPAddress.Loopback); }
    public static bool Case1602() { var value = new IPEndPoint(IPAddress.IPv6Loopback, 1602); return value.AddressFamily == AddressFamily.InterNetworkV6; }
    public static bool Case1603() { var value = new DnsEndPoint("example.test", 1603); return value.Host == "example.test" && value.Port == 1603; }
    public static bool Case1604() { var value = new UnixDomainSocketEndPoint("/tmp/1604"); return value.AddressFamily == AddressFamily.Unix; }
    public static bool Case1605() { var value = new SocketAddress(AddressFamily.InterNetwork, 16); return value.Family == AddressFamily.InterNetwork && value.Size == 16; }
    public static bool Case1606() { var value = new Uri("http://example.test:1606/path"); return value.Port == 1606; }
    public static bool Case1607() { var value = new UriBuilder("https", "example.test", 1607, "/x").Uri; return value.Scheme == "https"; }
    public static bool Case1608() { var value = new HttpRequestMessage(HttpMethod.Get, "https://example.test/1608"); return value.Method == HttpMethod.Get && value.RequestUri!.AbsolutePath == "/1608"; }
    public static bool Case1609() { using var value = new HttpRequestMessage(HttpMethod.Post, "https://example.test"); value.Content = new StringContent("1609"); return value.Content.Headers.ContentType!.MediaType == "text/plain"; }
    public static bool Case1610() { using var value = new HttpResponseMessage(HttpStatusCode.OK); value.Content = new StringContent("1610"); return value.IsSuccessStatusCode && value.Content.Headers.ContentLength > 0; }
    public static bool Case1611() { using var value = new HttpRequestMessage(HttpMethod.Get, "https://example.test"); value.Headers.Add("X-Case", "1611"); return value.Headers.GetValues("X-Case").Single() == "1611"; }
    public static bool Case1612() { using var value = new HttpResponseMessage(HttpStatusCode.Accepted); value.Headers.Add("X-Case", "1612"); return value.Headers.Contains("X-Case"); }
    public static bool Case1613() { var value = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"); value.CharSet = "utf-8"; return value.MediaType == "application/json" && value.CharSet == "utf-8"; }
    public static bool Case1614() { var value = System.Net.Http.Headers.ProductInfoHeaderValue.Parse("SharpHide/1614"); return value.Product.Name == "SharpHide"; }
    public static bool Case1615() { var value = System.Net.Http.Headers.RangeHeaderValue.Parse("bytes=0-1615"); return value.Ranges.Single().From == 0 && value.Ranges.Single().To == 1615; }
    public static bool Case1616() { var value = System.Net.Http.Headers.EntityTagHeaderValue.Parse("\"1616\""); return value.Tag == "\"1616\""; }
    public static bool Case1617() { var value = System.Net.Http.Headers.NameValueHeaderValue.Parse("x=1617"); return value.Name == "x" && value.Value == "1617"; }
    public static bool Case1618() { var value = System.Net.Http.Headers.ContentRangeHeaderValue.Parse("bytes 0-1618/2000"); return value.From == 0 && value.To == 1618; }
    public static bool Case1619() { var value = new Cookie("id", "1619"); return value.Name == "id" && value.Value == "1619"; }
    public static bool Case1620() { var value = new CookieContainer(); value.Add(new Uri("https://example.test"), new Cookie("id", "1620")); return value.GetCookies(new Uri("https://example.test"))["id"]!.Value == "1620"; }
    public static bool Case1621() { var value = IPAddress.Parse("192.168.1.1").GetAddressBytes(); return value.SequenceEqual(new byte[] { 192, 168, 1, 1 }); }
    public static bool Case1622() { var value = IPAddress.Parse("2001:db8::1622"); return value.AddressFamily == AddressFamily.InterNetworkV6; }
    public static bool Case1623() { var value = IPAddress.IsLoopback(IPAddress.Loopback); return value; }
    public static bool Case1624() { var value = IPAddress.Any; return value.Equals(IPAddress.Parse("0.0.0.0")); }
    public static bool Case1625() { var value = IPAddress.Broadcast; return value.GetAddressBytes()[3] == 255; }
    public static bool Case1626() { var value = IPAddress.HostToNetworkOrder(1626); return IPAddress.NetworkToHostOrder(value) == 1626; }
    public static bool Case1627() { var value = IPAddress.HostToNetworkOrder((short)1627); return IPAddress.NetworkToHostOrder(value) == 1627; }
    public static bool Case1628() { var value = new NetworkCredential("user", "1628"); return value.UserName == "user" && value.Password == "1628"; }
    public static bool Case1629() { var value = new Uri("mailto:1629@example.test"); return value.Scheme == "mailto"; }
    public static bool Case1630() { var value = new System.Net.Mail.MailAddress("1630@example.test"); return value.Host == "example.test"; }
    public static bool Case1631() { var value = new WebHeaderCollection(); value.Add("X-Case", "1631"); return value["X-Case"] == "1631"; }
    public static bool Case1632() { var value = new WebHeaderCollection { ["X-Case"] = "1632" }; return value.AllKeys.Contains("X-Case"); }
    public static bool Case1633() { var value = new WebProxy("http://proxy.example.test:1633"); return value.Address!.Port == 1633; }
    public static bool Case1634() { var value = new Uri("http://user:pass@example.test:1634/path"); return value.UserInfo == "user:pass"; }
    public static bool Case1635() { var value = Uri.EscapeDataString("a b/1635"); return value == "a%20b%2F1635"; }
    public static bool Case1636() { var value = Uri.UnescapeDataString("a%20b%2F1636"); return value == "a b/1636"; }
    public static bool Case1637() { var value = Uri.CheckHostName("127.0.0.1"); return value == UriHostNameType.IPv4; }
    public static bool Case1638() { var value = Uri.CheckHostName("2001:db8::1"); return value == UriHostNameType.IPv6; }
    public static bool Case1639() { var value = new Uri("https://example.test/1639").GetLeftPart(UriPartial.Authority); return value == "https://example.test"; }
    public static bool Case1640() { var value = new Uri("https://example.test/1640?a=1#frag"); return value.Fragment == "#frag" && value.Query == "?a=1"; }
    public static bool Case1641() { var value = new Uri("https://example.test").IsDefaultPort; return value; }
    public static bool Case1642() { var value = new Uri("https://example.test:1642"); return value.IsDefaultPort == false && value.Port == 1642; }
    public static bool Case1643() { var value = new UriBuilder("https", "example.test", -1, "1643"); return value.Uri.AbsolutePath == "/1643"; }
    public static bool Case1644() { var value = new StringContent("1644", Encoding.UTF8, "text/plain"); return value.Headers.ContentType!.CharSet == "utf-8"; }
    public static bool Case1645() { var value = new ByteArrayContent(Encoding.UTF8.GetBytes("1645")); return value.Headers.ContentLength == 4; }
    public static bool Case1646() { var value = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("id", "1646") }); return value.Headers.ContentType!.MediaType == "application/x-www-form-urlencoded"; }
    public static bool Case1647() { var value = new MultipartFormDataContent("1647"); return value.Headers.ContentType!.MediaType == "multipart/form-data"; }
    public static bool Case1648() { var value = new HttpMethod("PATCH"); return value.Method == "PATCH"; }
    public static bool Case1649() { var value = HttpMethod.Delete; return value.Method == "DELETE"; }
    public static bool Case1650() { var value = (HttpStatusCode)418; return (int)value == 418; }
}
