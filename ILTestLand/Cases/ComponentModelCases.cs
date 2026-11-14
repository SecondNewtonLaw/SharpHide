using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class ComponentModelCases
{
    public static bool Case1751() { var value = TypeDescriptor.GetConverter(typeof(int)).ConvertFromInvariantString("1751"); return value is 1751; }
    public static bool Case1752() { var value = TypeDescriptor.GetConverter(typeof(bool)).ConvertFromInvariantString("true"); return value is true; }
    public static bool Case1753() { var value = TypeDescriptor.GetConverter(typeof(Guid)).ConvertFromInvariantString("00000000-0000-0000-0000-000000001753"); return Convert.ToString(value, CultureInfo.InvariantCulture)!.EndsWith("1753", StringComparison.Ordinal); }
    public static bool Case1754() { var value = TypeDescriptor.GetConverter(typeof(DateTime)).ConvertFromInvariantString("2026-09-08"); return Convert.ToString(value, CultureInfo.InvariantCulture)!.StartsWith("09/08/2026", StringComparison.Ordinal); }
    public static bool Case1755() { var value = TypeDescriptor.GetConverter(typeof(Version)).ConvertFromInvariantString("17.55"); return value is Version version && version.Minor == 55; }
    public static bool Case1756() { var value = TypeDescriptor.GetConverter(typeof(Uri)).ConvertFromInvariantString("https://example.test/1756"); return value is Uri uri && uri.AbsolutePath == "/1756"; }
    public static bool Case1757() { var value = TypeDescriptor.GetConverter(typeof(decimal)).ConvertFromInvariantString("1757.5"); return Convert.ToString(value, CultureInfo.InvariantCulture) == "1757.5"; }
    public static bool Case1758() { var value = TypeDescriptor.GetConverter(typeof(char)).ConvertFromInvariantString("A"); return value is 'A'; }
    public static bool Case1759() { var value = TypeDescriptor.GetConverter(typeof(TimeSpan)).ConvertFromInvariantString("00:17:59"); return Convert.ToString(value, CultureInfo.InvariantCulture) == "00:17:59"; }
    public static bool Case1760() { var value = TypeDescriptor.GetConverter(typeof(Status)).ConvertFromInvariantString("Open"); return value is Status.Open; }
    public static bool Case1761() { var value = new Claim(ClaimTypes.Name, "1761"); return value.Type == ClaimTypes.Name && value.Value == "1761"; }
    public static bool Case1762() { var identity = new ClaimsIdentity("test"); identity.AddClaim(new Claim("id", "1762")); return identity.FindFirst("id")!.Value == "1762"; }
    public static bool Case1763() { var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "admin")], "test")); return principal.Identity!.IsAuthenticated && principal.IsInRole("admin"); }
    public static bool Case1764() { var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("id", "1764")], "test")); return principal.FindFirst("id")!.Value == "1764"; }
    public static bool Case1765() { var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "user")], "test")); return principal.IsInRole("user"); }
    public static bool Case1766() { var identity = new ClaimsIdentity(); identity.AddClaim(new Claim("id", "1766")); identity.RemoveClaim(identity.FindFirst("id")!); return identity.FindFirst("id") == null; }
    public static bool Case1767() { var value = new ValidationContext(new object()); return value.ObjectType == typeof(object); }
    public static bool Case1768() { var value = new ValidationResult("1768", ["Value"]); return value.MemberNames.Single() == "Value"; }
    public static bool Case1769() { var value = new RequiredAttribute(); return value.IsValid("1769"); }
    public static bool Case1770() { var value = new RangeAttribute(0, 1770); return value.IsValid(1770); }
    public static bool Case1771() { var value = new StringLengthAttribute(10) { MinimumLength = 4 }; return value.IsValid("1771"); }
    public static bool Case1772() { var value = new RegularExpressionAttribute("^\\d+$"); return value.IsValid("1772"); }
    public static bool Case1773() { var value = new CompareAttribute("Other"); return value != null; }
    public static bool Case1774() { var value = new EmailAddressAttribute(); return value.IsValid("1774@example.test"); }
    public static bool Case1775() { var value = new UrlAttribute(); return value.IsValid("https://example.test/1775"); }
    public static bool Case1776() { var value = new PhoneAttribute(); return value.IsValid("+1 1776"); }
    public static bool Case1777() { var value = new CreditCardAttribute(); return !value.IsValid("1777"); }
    public static bool Case1778() { var value = new DisplayNameAttribute("1778"); return value.DisplayName == "1778"; }
    public static bool Case1779() { var value = new CategoryAttribute("1779"); return value.Category == "1779"; }
    public static bool Case1780() { var value = new DescriptionAttribute("1780"); return value.Description == "1780"; }
    public static bool Case1781() { var value = new DefaultValueAttribute(1781); return value.Value is 1781; }
    public static bool Case1782() { var value = new BrowsableAttribute(true); return value.Browsable; }
    public static bool Case1783() { var value = new ReadOnlyAttribute(true); return value.IsReadOnly; }
    public static bool Case1784() { var value = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden); return value.Visibility == DesignerSerializationVisibility.Hidden; }
    public static bool Case1785() { var value = new EditorBrowsableAttribute(EditorBrowsableState.Never); return value.State == EditorBrowsableState.Never; }
    public static bool Case1786() { var value = new AttributeCollection(new Attribute[] { new DescriptionAttribute("1786") }); return value[typeof(DescriptionAttribute)]!.IsDefaultAttribute() == false; }
    public static bool Case1787() { var value = new PropertyDescriptorCollection([]); return value.Count == 0; }
    public static bool Case1788() { var value = new EventDescriptorCollection([]); return value.Count == 0; }
    public static bool Case1789() { var value = TypeDescriptor.GetProvider(typeof(ComponentModelCases)); return value.GetReflectionType(typeof(ComponentModelCases)) == typeof(ComponentModelCases); }
    public static bool Case1790() { var value = TypeDescriptor.GetProperties(typeof(PropertyBag)); return value[nameof(PropertyBag.Value)] != null; }
    public static bool Case1791() { var value = new PropertyBag { Value = 1791 }; return TypeDescriptor.GetProperties(value)[nameof(PropertyBag.Value)]!.GetValue(value) is 1791; }
    public static bool Case1792() { var value = TypeDescriptor.GetConverter(typeof(Status)); return value.CanConvertFrom(typeof(string)); }
    public static bool Case1793() { var value = TypeDescriptor.GetConverter(typeof(int)); return value.CanConvertTo(typeof(string)); }
    public static bool Case1794() { var value = TypeDescriptor.GetConverter(typeof(int)).ConvertToInvariantString(1794); return value == "1794"; }
    public static bool Case1795() { var value = TypeDescriptor.GetConverter(typeof(bool)).ConvertToInvariantString(true); return value == "True"; }
    public static bool Case1796() { var value = new CultureInfo("en-US"); return value.DateTimeFormat.ShortDatePattern.Length > 0; }
    public static bool Case1797() { var value = CultureInfo.InvariantCulture.CompareInfo.Compare("a", "A", CompareOptions.IgnoreCase); return value == 0; }
    public static bool Case1798() { var value = CultureInfo.InvariantCulture.TextInfo.ListSeparator; return value == ","; }
    public static bool Case1799() { var value = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator; return value == "."; }
    public static bool Case1800() { var value = CultureInfo.InvariantCulture.Calendar; return value is GregorianCalendar; }

    private enum Status { Closed, Open }
    private sealed class PropertyBag { public int Value { get; set; } }
}
