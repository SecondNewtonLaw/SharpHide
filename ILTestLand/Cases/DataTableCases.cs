using System.Data;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class DataTableCases
{
    public static bool Case1701() { var table = Create(); return table.Columns.Count == 2; }
    public static bool Case1702() { var table = Create(); return table.Rows.Count == 3; }
    public static bool Case1703() { var table = Create(); return (int)table.Rows[0]["Id"] == 1703; }
    public static bool Case1704() { var table = Create(); return table.Rows[1]["Name"].ToString() == "b"; }
    public static bool Case1705() { var table = Create(); return Convert.ToInt32(table.Compute("SUM(Id)", "")) == 5130; }
    public static bool Case1706() { var table = Create(); return Convert.ToInt32(table.Compute("COUNT(Id)", "")) == 3; }
    public static bool Case1707() { var table = Create(); return table.Compute("MAX(Id)", "") is 1718; }
    public static bool Case1708() { var table = Create(); return table.Select("Id > 1707").Length == 2; }
    public static bool Case1709() { var table = Create(); return table.Select("Name = 'b'").Single()["Id"] is 1709; }
    public static bool Case1710() { var table = Create(); return table.Select("Id > 1709", "Id DESC").First()["Id"] is 1718; }
    public static bool Case1711() { var table = Create(); var view = new DataView(table) { RowFilter = "Id >= 1711" }; return view.Count == 1; }
    public static bool Case1712() { var table = Create(); var view = new DataView(table) { Sort = "Name DESC" }; return view[0]["Name"].ToString() == "c"; }
    public static bool Case1713() { var table = Create(); var view = new DataView(table); return view.ToTable().Rows.Count == 3; }
    public static bool Case1714() { var table = Create(); table.PrimaryKey = [table.Columns["Id"]!]; return table.Rows.Find(1703)!["Name"].ToString() == "a"; }
    public static bool Case1715() { var table = Create(); table.AcceptChanges(); table.Rows[0]["Name"] = "1715"; return table.Rows[0].RowState == DataRowState.Modified; }
    public static bool Case1716() { var table = Create(); table.AcceptChanges(); table.Rows[0].Delete(); return table.Rows[0].RowState == DataRowState.Deleted; }
    public static bool Case1717() { var table = Create(); table.AcceptChanges(); table.Rows[0]["Name"] = "1717"; table.RejectChanges(); return table.Rows[0]["Name"].ToString() == "a"; }
    public static bool Case1718() { var table = Create(); table.AcceptChanges(); table.Rows[0]["Name"] = "1718"; return table.GetChanges() != null; }
    public static bool Case1719() { var table = Create(); table.AcceptChanges(); return table.GetChanges() == null; }
    public static bool Case1720() { var table = Create(); return table.Rows.Cast<DataRow>().Sum(row => (int)row["Id"]) == 5130; }
    public static bool Case1721() { var table = Create(); return table.AsEnumerable().Select(row => row.Field<int>("Id")).Min() == 1703; }
    public static bool Case1722() { var table = Create(); return table.AsEnumerable().Select(row => row.Field<string>("Name")).Order().SequenceEqual(new[] { "a", "b", "c" }); }
    public static bool Case1723() { var table = Create(); return table.AsEnumerable().Where(row => row.Field<int>("Id") % 2 == 1).Count() == 2; }
    public static bool Case1724() { var table = Create(); return table.AsEnumerable().Select(row => row.Field<int>("Id")).Average() == 1710; }
    public static bool Case1725() { var table = Create(); return table.AsEnumerable().GroupBy(row => row.Field<string>("Name")![0]).Count() == 3; }
    public static bool Case1726() { var table = Create(); var copy = table.Copy(); return copy.TableName == table.TableName && copy.Rows.Count == 3; }
    public static bool Case1727() { var table = Create(); var clone = table.Clone(); return clone.Columns.Count == table.Columns.Count && clone.Rows.Count == 0; }
    public static bool Case1728() { var table = Create(); var row = table.NewRow(); row["Id"] = 1728; row["Name"] = "x"; table.Rows.Add(row); return table.Rows.Count == 4; }
    public static bool Case1729() { var table = Create(); table.Rows.Add(1729, "x"); return table.Rows[^1]["Name"].ToString() == "x"; }
    public static bool Case1730() { var table = Create(); table.Columns.Add("Value", typeof(int)); table.Rows[0]["Value"] = 1730; return table.Rows[0].Field<int>("Value") == 1730; }
    public static bool Case1731() { var table = Create(); table.Columns["Name"]!.AllowDBNull = false; return !table.Columns["Name"]!.AllowDBNull; }
    public static bool Case1732() { var table = Create(); table.Columns["Id"]!.Unique = true; return table.Columns["Id"]!.Unique; }
    public static bool Case1733() { var table = Create(); table.Columns["Name"]!.Caption = "Label"; return table.Columns["Name"]!.Caption == "Label"; }
    public static bool Case1734() { var table = Create(); table.Constraints.Add("id", table.Columns["Id"]!, true); return table.Constraints.Count == 1; }
    public static bool Case1735() { var set = new DataSet(); var table = Create(); set.Tables.Add(table); return set.Tables[0].Rows.Count == 3; }
    public static bool Case1736() { var set = new DataSet(); set.Tables.Add(Create()); set.Tables.Add(CreateOther()); return set.Tables.Count == 2; }
    public static bool Case1737() { var set = new DataSet(); var first = Create(); var second = CreateOther(); set.Tables.Add(first); set.Tables.Add(second); set.Relations.Add("rel", first.Columns["Id"]!, second.Columns["ParentId"]!); return set.Relations.Count == 1; }
    public static bool Case1738() { var set = new DataSet(); set.Tables.Add(Create()); using var writer = new StringWriter(); set.WriteXml(writer, XmlWriteMode.WriteSchema); return writer.ToString().Contains("Id", StringComparison.Ordinal); }
    public static bool Case1739() { var table = Create(); using var writer = new StringWriter(); table.WriteXml(writer, XmlWriteMode.IgnoreSchema); return writer.ToString().Contains("1703", StringComparison.Ordinal); }
    public static bool Case1740() { var table = Create(); using var writer = new StringWriter(); table.WriteXmlSchema(writer); return writer.ToString().Contains("DataTable", StringComparison.Ordinal); }
    public static bool Case1741() { var table = Create(); using var reader = new StringReader("<NewDataSet><Row><Id>1741</Id><Name>x</Name></Row></NewDataSet>"); var result = new DataSet(); result.ReadXml(reader); return result.Tables.Count == 1; }
    public static bool Case1742() { var table = Create(); var xml = table.ToXml(); return xml.Contains("1703", StringComparison.Ordinal); }
    public static bool Case1743() { var table = Create(); return table.Namespace == ""; }
    public static bool Case1744() { var table = Create(); table.Namespace = "urn:1744"; return table.Namespace == "urn:1744"; }
    public static bool Case1745() { var table = Create(); table.Prefix = "p"; return table.Prefix == "p"; }
    public static bool Case1746() { var table = Create(); return table.Locale != null; }
    public static bool Case1747() { var table = Create(); table.CaseSensitive = true; return table.CaseSensitive; }
    public static bool Case1748() { var table = Create(); table.ExtendedProperties["id"] = 1748; return (int)table.ExtendedProperties["id"]! == 1748; }
    public static bool Case1749() { var table = Create(); return table.Rows.Cast<DataRow>().Select(row => row.ItemArray.Length).All(length => length == 2); }
    public static bool Case1750() { var table = Create(); return table.Rows.Cast<DataRow>().Select(row => row.Table == table).All(value => value); }

    private static DataTable Create()
    {
        var table = new DataTable("Rows");
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(1703, "a");
        table.Rows.Add(1709, "b");
        table.Rows.Add(1718, "c");
        return table;
    }

    private static DataTable CreateOther()
    {
        var table = new DataTable("Other");
        table.Columns.Add("ParentId", typeof(int));
        table.Rows.Add(1703);
        return table;
    }

    private static string ToXml(this DataTable table) { using var writer = new StringWriter(); table.WriteXml(writer, XmlWriteMode.IgnoreSchema); return writer.ToString(); }
}
