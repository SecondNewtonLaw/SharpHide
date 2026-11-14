using System.Xml;
using System.Xml.Linq;
using System.Text;

internal static class XmlDomCases
{
    public static bool XmlDom_2601() { var value = XElement.Parse("<root><item>2601</item></root>"); return value.Element("item")!.Value == "2601"; }
    public static bool XmlDom_2602() { var value = new XElement("root", new XElement("item", 2602)); return value.ToString().Contains("2602"); }
    public static bool XmlDom_2603() { var value = XElement.Parse("<root id='2603'/>"); return value.Attribute("id")!.Value == "2603"; }
    public static bool XmlDom_2604() { var value = XElement.Parse("<root><a/><b/><c/></root>"); return value.Elements().Count() == 3; }
    public static bool XmlDom_2605() { var value = XElement.Parse("<root><a/><b/></root>"); value.Add(new XElement("c")); return value.Elements().Last().Name == "c"; }
    public static bool XmlDom_2606() { var value = XElement.Parse("<root><a/><b/></root>"); value.Element("a")!.Remove(); return value.Elements().Count() == 1; }
    public static bool XmlDom_2607() { var value = XElement.Parse("<root><a>1</a><a>2</a></root>"); return value.Elements("a").Sum(item => (int)item) == 3; }
    public static bool XmlDom_2608() { var value = XElement.Parse("<root><a>2608</a></root>"); return (string)value.Element("a")! == "2608"; }
    public static bool XmlDom_2609() { var value = XElement.Parse("<root><a>2609</a></root>"); return (int)value.Element("a")! == 2609; }
    public static bool XmlDom_2610() { var value = XElement.Parse("<root><a>true</a></root>"); return (bool)value.Element("a")!; }
    public static bool XmlDom_2611() { var value = XElement.Parse("<root><a>2611</a></root>"); return value.Descendants("a").Single().Value == "2611"; }
    public static bool XmlDom_2612() { var value = XElement.Parse("<root><a/></root>"); return value.Descendants().Count() == 1; }
    public static bool XmlDom_2613() { var value = XElement.Parse("<root><a/></root>"); return value.Element("a")!.Ancestors().Single().Name == "root"; }
    public static bool XmlDom_2614() { var value = XElement.Parse("<root><a/></root>"); return value.Element("a")!.Parent!.Name == "root"; }
    public static bool XmlDom_2615() { var value = XElement.Parse("<root><a/></root>"); return value.Nodes().Count() == 1; }
    public static bool XmlDom_2616() { var value = XElement.Parse("<root>text</root>"); return value.Nodes().OfType<XText>().Single().Value == "text"; }
    public static bool XmlDom_2617() { var value = XNamespace.Get("urn:2617"); return new XElement(value + "root").Name.Namespace == value; }
    public static bool XmlDom_2618() { var value = XName.Get("item", "urn:2618"); return value.LocalName == "item"; }
    public static bool XmlDom_2619() { var value = new XComment("2619"); return value.Value == "2619"; }
    public static bool XmlDom_2620() { var value = new XCData("2620"); return value.Value == "2620"; }
    public static bool XmlDom_2621() { var value = new XDeclaration("1.0", "utf-8", "yes"); return value.Version == "1.0"; }
    public static bool XmlDom_2622() { var value = new XmlDocument(); value.LoadXml("<root><item>2622</item></root>"); return value.SelectSingleNode("/root/item")!.InnerText == "2622"; }
    public static bool XmlDom_2623() { var value = new XmlDocument(); value.LoadXml("<root id='2623'/>"); return value.DocumentElement!.GetAttribute("id") == "2623"; }
    public static bool XmlDom_2624() { var value = new XmlDocument(); value.LoadXml("<root><a/><b/></root>"); return value.DocumentElement!.ChildNodes.Count == 2; }
    public static bool XmlDom_2625() { var value = new XmlDocument(); value.LoadXml("<root/>"); var item = value.CreateElement("item"); value.DocumentElement!.AppendChild(item); return value.DocumentElement.ChildNodes.Count == 1; }
    public static bool XmlDom_2626() { var value = new XmlDocument(); value.LoadXml("<root><a/></root>"); return value.GetElementsByTagName("a").Count == 1; }
    public static bool XmlDom_2627() { var value = new XmlDocument(); value.LoadXml("<root>2627</root>"); return value.DocumentElement!.FirstChild!.NodeType == XmlNodeType.Text; }
    public static bool XmlDom_2628() { var value = new XmlDocument(); value.LoadXml("<root/>"); return value.CreateAttribute("id").NodeType == XmlNodeType.Attribute; }
    public static bool XmlDom_2629() { var value = new XmlDocument(); value.LoadXml("<root/>"); return value.CreateComment("2629").NodeType == XmlNodeType.Comment; }
    public static bool XmlDom_2630() { var value = new XmlDocument(); value.LoadXml("<root/>"); return value.CreateCDataSection("2630").NodeType == XmlNodeType.CDATA; }
    public static bool XmlDom_2631() { var value = new XmlDocument(); value.LoadXml("<root/>"); return value.OuterXml == "<root />"; }
    public static bool XmlDom_2632() { var value = new XmlDocument(); value.LoadXml("<root><x>2632</x></root>"); return value.InnerText == "2632"; }
    public static bool XmlDom_2633() { using var value = new StringReader("<root>2633</root>"); using var reader = XmlReader.Create(value); reader.MoveToContent(); return reader.Name == "root"; }
    public static bool XmlDom_2634() { using var value = new StringReader("<root>2634</root>"); using var reader = XmlReader.Create(value); return reader.ReadToFollowing("root"); }
    public static bool XmlDom_2635() { using var value = new StringReader("<root id='2635'/>"); using var reader = XmlReader.Create(value); reader.ReadToFollowing("root"); return reader.GetAttribute("id") == "2635"; }
    public static bool XmlDom_2636() { using var value = new StringReader("<root><item/></root>"); using var reader = XmlReader.Create(value); var count = 0; while (reader.Read()) if (reader.NodeType == XmlNodeType.Element) count++; return count == 2; }
    public static bool XmlDom_2637() { var value = new StringBuilder(); using (var writer = XmlWriter.Create(value, new XmlWriterSettings { OmitXmlDeclaration = true })) { writer.WriteStartElement("root"); writer.WriteString("2637"); writer.WriteEndElement(); } return value.ToString().Contains("2637"); }
    public static bool XmlDom_2638() { var value = new StringBuilder(); using (var writer = XmlWriter.Create(value, new XmlWriterSettings { OmitXmlDeclaration = true })) writer.WriteElementString("item", "2638"); return value.ToString().Contains("2638"); }
    public static bool XmlDom_2639() { var value = new StringBuilder(); using (var writer = XmlWriter.Create(value, new XmlWriterSettings { OmitXmlDeclaration = true })) { writer.WriteStartElement("root"); writer.WriteAttributeString("id", "2639"); writer.WriteEndElement(); } return value.ToString().Contains("2639"); }
    public static bool XmlDom_2640() { var value = new StringBuilder(); using (var writer = XmlWriter.Create(value, new XmlWriterSettings { OmitXmlDeclaration = true })) writer.WriteComment("2640"); return value.ToString().Contains("2640"); }
    public static bool XmlDom_2641() { var value = XDocument.Parse("<root><a>2641</a></root>"); return value.Root!.Name == "root"; }
    public static bool XmlDom_2642() { var value = new XDocument(new XElement("root", 2642)); return value.Root!.Value == "2642"; }
    public static bool XmlDom_2643() { var value = XDocument.Parse("<root><a/><b/></root>"); return value.Root!.Elements().Count() == 2; }
    public static bool XmlDom_2644() { var value = XDocument.Parse("<root/>"); return value.Declaration == null; }
    public static bool XmlDom_2645() { var value = XDocument.Parse("<!--2645--><root/>"); return value.Nodes().OfType<XComment>().Single().Value == "2645"; }
    public static bool XmlDom_2646() { var value = XDocument.Parse("<root/>", LoadOptions.PreserveWhitespace); return value.Root != null; }
    public static bool XmlDom_2647() { var value = XElement.Parse("<root><a>2647</a></root>"); return value.ToString(SaveOptions.DisableFormatting).Contains("2647"); }
    public static bool XmlDom_2648() { var value = XElement.Parse("<root><a>2648</a></root>"); return XElement.Parse(value.ToString()).Value == "2648"; }
    public static bool XmlDom_2649() { var value = XElement.Parse("<root><a>2649</a></root>"); return XNode.DeepEquals(value, XElement.Parse(value.ToString())); }
    public static bool XmlDom_2650() { var value = XElement.Parse("<root/>"); return value.IsEmpty; }
}
