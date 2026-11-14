using System.Runtime.Serialization;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class SerializationContractCases
{
    public static bool SerializationContract_1851() { return XmlConvert.ToString(1851) == "1851"; }
    public static bool SerializationContract_1852() { return XmlConvert.ToInt32("1852") == 1852; }
    public static bool SerializationContract_1853() { return 18.53m.ToString(CultureInfo.InvariantCulture) == "18.53"; }
    public static bool SerializationContract_1854() { var value = XmlConvert.ToString(new DateTime(2026, 9, 8), XmlDateTimeSerializationMode.Utc); return value.StartsWith("2026-09-08", StringComparison.Ordinal); }
    public static bool SerializationContract_1855() { var value = XmlConvert.ToTimeSpan("PT18H55M"); return value.TotalMinutes == 1135; }
    public static bool SerializationContract_1856() { var value = XmlConvert.ToGuid("00000000-0000-0000-0000-000000001856"); return value != Guid.Empty; }
    public static bool SerializationContract_1857() { var value = Convert.ToBase64String([18, 57]); return value == "Ejk="; }
    public static bool SerializationContract_1858() { var value = Convert.FromBase64String("Ejg="); return value.SequenceEqual(new byte[] { 18, 56 }); }
    public static bool SerializationContract_1859() { var value = XmlConvert.VerifyNCName("case1859"); return value == "case1859"; }
    public static bool SerializationContract_1860() { var value = XmlConvert.EncodeName("case 1860"); return value.Contains("_x0020_", StringComparison.Ordinal); }
    public static bool SerializationContract_1861() { var value = JsonValue.Create(1861); return value!.GetValue<int>() == 1861; }
    public static bool SerializationContract_1862() { var value = JsonValue.Create("1862"); return value!.GetValue<string>() == "1862"; }
    public static bool SerializationContract_1863() { var value = new JsonObject { ["Id"] = 1863 }; return value["Id"]!.GetValue<int>() == 1863; }
    public static bool SerializationContract_1864() { var value = new JsonObject { ["id"] = 1864, ["ok"] = true }; return value["ok"]!.GetValue<bool>(); }
    public static bool SerializationContract_1865() { var value = new JsonArray(1865, 1, 2); return value[0]!.GetValue<int>() == 1865; }
    public static bool SerializationContract_1866() { var value = JsonNode.Parse("{\"items\":[1,2,3]}")!; return value["items"]!.AsArray().Count == 3; }
    public static bool SerializationContract_1867() { var value = JsonNode.Parse("null"); return value == null; }
    public static bool SerializationContract_1868() { var value = JsonSerializer.Serialize(new Contract(1868, "x")); return value.Contains("1868", StringComparison.Ordinal); }
    public static bool SerializationContract_1869() { var value = JsonSerializer.Deserialize<Dictionary<string, int>>("{\"id\":1869}")!; return value["id"] == 1869; }
    public static bool SerializationContract_1870() { var value = JsonSerializer.Deserialize<int[]>("[1,2,1870]")!; return value[^1] == 1870; }
    public static bool SerializationContract_1871() { var options = new JsonSerializerOptions { WriteIndented = true }; return JsonSerializer.Serialize(new Contract(1871, "x"), options).Contains("\n", StringComparison.Ordinal); }
    public static bool SerializationContract_1872() { var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; var value = JsonSerializer.Deserialize<Contract>("{\"id\":1872,\"name\":\"x\"}", options)!; return value.Id == 1872; }
    public static bool SerializationContract_1873() { var value = JsonDocument.Parse("{\"id\":1873}"); using (value) return value.RootElement.GetProperty("id").GetInt32() == 1873; }
    public static bool SerializationContract_1874() { using var value = JsonDocument.Parse("[1874]"); return value.RootElement[0].GetInt32() == 1874; }
    public static bool SerializationContract_1875() { using var value = JsonDocument.Parse("true"); return value.RootElement.GetBoolean(); }
    public static bool SerializationContract_1876() { using var value = JsonDocument.Parse("\"1876\""); return value.RootElement.GetString() == "1876"; }
    public static bool SerializationContract_1877() { var value = JsonDocument.Parse("{\"id\":1877}").RootElement.Clone(); return value.GetProperty("id").GetInt32() == 1877; }
    public static bool SerializationContract_1878() { var value = new DataContractSerializer(typeof(Contract)); using var stream = new MemoryStream(); value.WriteObject(stream, new Contract(1878, "x")); return stream.Length > 0; }
    public static bool SerializationContract_1879() { var serializer = new DataContractSerializer(typeof(Contract)); using var stream = new MemoryStream(); serializer.WriteObject(stream, new Contract(1879, "x")); stream.Position = 0; var value = (Contract)serializer.ReadObject(stream)!; return value.Id == 1879; }
    public static bool SerializationContract_1880() { var serializer = new DataContractSerializer(typeof(List<int>)); using var stream = new MemoryStream(); serializer.WriteObject(stream, new List<int> { 1880 }); stream.Position = 0; var value = (List<int>)serializer.ReadObject(stream)!; return value.Single() == 1880; }
    public static bool SerializationContract_1881() { var serializer = new DataContractSerializer(typeof(Dictionary<string, int>)); using var stream = new MemoryStream(); serializer.WriteObject(stream, new Dictionary<string, int> { ["x"] = 1881 }); stream.Position = 0; var value = (Dictionary<string, int>)serializer.ReadObject(stream)!; return value["x"] == 1881; }
    public static bool SerializationContract_1882() { var value = JsonSerializer.Serialize(new Contract(1882, "x")); return value.Contains("1882", StringComparison.Ordinal); }
    public static bool SerializationContract_1883() { var value = JsonSerializer.Deserialize<Contract>("{\"Id\":1883,\"Name\":\"x\"}")!; return value.Id == 1883; }
    public static bool SerializationContract_1884() { var value = new XmlDocument(); value.LoadXml("<root><id>1884</id></root>"); return value.SelectSingleNode("/root/id")!.InnerText == "1884"; }
    public static bool SerializationContract_1885() { var value = new XmlDocument(); value.LoadXml("<root id='1885'/>"); return value.DocumentElement!.GetAttribute("id") == "1885"; }
    public static bool SerializationContract_1886() { var value = new XmlDocument(); var root = value.CreateElement("root"); root.InnerText = "1886"; value.AppendChild(root); return value.DocumentElement!.InnerText == "1886"; }
    public static bool SerializationContract_1887() { var value = new XmlDocument(); value.LoadXml("<root><a/><a/></root>"); return value.SelectNodes("/root/a")!.Count == 2; }
    public static bool SerializationContract_1888() { var value = new XmlDocument(); value.LoadXml("<root><!--1888--></root>"); return value.DocumentElement!.FirstChild is XmlComment; }
    public static bool SerializationContract_1889() { var value = new XmlDocument(); value.LoadXml("<root><![CDATA[1889]]></root>"); return value.DocumentElement!.InnerText == "1889"; }
    public static bool SerializationContract_1890() { var value = new XmlDocument(); value.LoadXml("<root><id>1890</id></root>"); return value.OuterXml.Contains("1890", StringComparison.Ordinal); }
    public static bool SerializationContract_1891() { var value = new XmlNamespaceManager(new NameTable()); value.AddNamespace("x", "urn:1891"); return value.LookupNamespace("x") == "urn:1891"; }
    public static bool SerializationContract_1892() { var value = new NameTable(); var text = value.Add("1892"); return value.Get("1892") == text; }
    public static bool SerializationContract_1893() { var value = new XmlQualifiedName("id", "urn:1893"); return value.Namespace == "urn:1893"; }
    public static bool SerializationContract_1894() { var value = new XmlQualifiedName("id", "urn:1894"); return value.Name == "id"; }
    public static bool SerializationContract_1895() { var value = new XmlReaderSettings { IgnoreComments = true }; return value.IgnoreComments; }
    public static bool SerializationContract_1896() { var value = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }; return value.Indent && value.Encoding != null; }
    public static bool SerializationContract_1897() { using var stream = new MemoryStream(); using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { OmitXmlDeclaration = true })) { writer.WriteStartElement("id"); writer.WriteString("1897"); writer.WriteEndElement(); } return Encoding.UTF8.GetString(stream.ToArray()).Contains("1897", StringComparison.Ordinal); }
    public static bool SerializationContract_1898() { var value = new System.Xml.Serialization.XmlSerializer(typeof(string)); using var stream = new MemoryStream(); value.Serialize(stream, "1898"); return stream.Length > 0; }
    public static bool SerializationContract_1899() { var value = new System.Xml.Serialization.XmlSerializer(typeof(string)); using var stream = new MemoryStream(); value.Serialize(stream, "1899"); stream.Position = 0; return (string)value.Deserialize(stream)! == "1899"; }
    public static bool SerializationContract_1900() { var value = new Contract(1900, "x"); return value.Id == 1900 && value.Name == "x"; }

    [DataContract]
    public sealed record Contract([property: DataMember] int Id, [property: DataMember] string Name)
    {
        public Contract() : this(0, "") { }
    }
}
