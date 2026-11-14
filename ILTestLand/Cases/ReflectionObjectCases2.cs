using System.Reflection;

internal static class ReflectionObjectCases2
{
    public static bool Case2551() { return typeof(string).IsClass; }
    public static bool Case2552() { return typeof(int).IsValueType; }
    public static bool Case2553() { return typeof(List<int>).IsGenericType; }
    public static bool Case2554() { return typeof(List<>).ContainsGenericParameters; }
    public static bool Case2555() { return typeof(int).IsPrimitive; }
    public static bool Case2556() { return typeof(IDisposable).IsInterface; }
    public static bool Case2557() { return typeof(Exception).IsAssignableFrom(typeof(InvalidOperationException)); }
    public static bool Case2558() { return typeof(List<int>).GetGenericArguments().Length == 1; }
    public static bool Case2559() { return typeof(Dictionary<,>).GetGenericArguments().Length == 2; }
    public static bool Case2560() { return typeof(int).MakeArrayType().GetElementType() == typeof(int); }
    public static bool Case2561() { return typeof(int).MakeByRefType().IsByRef; }
    public static bool Case2562() { return typeof(int).MakePointerType().IsPointer; }
    public static bool Case2563() { return typeof(int[,]).GetArrayRank() == 2; }
    public static bool Case2564() { return typeof(int[]).GetArrayRank() == 1; }
    public static bool Case2565() { return typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }) != null; }
    public static bool Case2566() { return typeof(string).GetProperty("Length") != null; }
    public static bool Case2567() { return typeof(string).GetField("Empty", BindingFlags.Public | BindingFlags.Static) != null; }
    public static bool Case2568() { return typeof(string).GetMembers().Length > 0; }
    public static bool Case2569() { return typeof(string).GetMethods().Any(method => method.Name == "ToString"); }
    public static bool Case2570() { return typeof(string).GetInterfaces().Length >= 1; }
    public static bool Case2571() { return typeof(string).BaseType == typeof(object); }
    public static bool Case2572() { return typeof(string).Assembly != null; }
    public static bool Case2573() { return typeof(string).Module != null; }
    public static bool Case2574() { return typeof(string).Namespace == "System"; }
    public static bool Case2575() { return typeof(string).FullName == "System.String"; }
    public static bool Case2576() { return typeof(string).Name == "String"; }
    public static bool Case2577() { return typeof(string).AssemblyQualifiedName!.Contains("System.String"); }
    public static bool Case2578() { return typeof(string).GetCustomAttributes().Count() >= 0; }
    public static bool Case2579() { return typeof(string).IsPublic; }
    public static bool Case2580() { return typeof(List<>).IsPublic; }
    public static bool Case2581() { var value = Activator.CreateInstance(typeof(List<int>)); return value is List<int>; }
    public static bool Case2582() { var value = Activator.CreateInstance(typeof(string), new object[] { new char[] { '2', '5', '8', '2' } }); return value is string; }
    public static bool Case2583() { var value = new List<int> { 1, 2 }; return value.GetType().GetProperty("Count")!.GetValue(value)!.Equals(2); }
    public static bool Case2584() { var value = new List<int>(); value.GetType().GetMethod("Add")!.Invoke(value, new object[] { 2584 }); return value.Single() == 2584; }
    public static bool Case2585() { var value = new { Id = 2585 }; return value.GetType().GetProperty("Id")!.GetValue(value)!.Equals(2585); }
    public static bool Case2586() { var value = typeof(string).GetMethod("IsNullOrEmpty")!; return (bool)value.Invoke(null, new object[] { "" })!; }
    public static bool Case2587() { var value = typeof(Math).GetMethod("Abs", new[] { typeof(int) })!; return (int)value.Invoke(null, new object[] { -2587 })! == 2587; }
    public static bool Case2588() { var value = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })!; return (string)value.Invoke(null, new object[] { "25", "88" })! == "2588"; }
    public static bool Case2589() { return typeof(List<int>).GetConstructor(Type.EmptyTypes) != null; }
    public static bool Case2590() { return typeof(List<int>).GetConstructors().Length > 0; }
    public static bool Case2591() { return typeof(List<int>).GetProperties().Any(property => property.Name == "Count"); }
    public static bool Case2592() { return typeof(List<int>).GetEvents().Length == 0; }
    public static bool Case2593() { return typeof(List<int>).GetNestedTypes().Length >= 0; }
    public static bool Case2594() { return typeof(List<int>).GetInterfaces().Contains(typeof(IList<int>)); }
    public static bool Case2595() { return typeof(List<int>).GetInterface("IList`1") != null; }
    public static bool Case2596() { return typeof(Dictionary<int, int>).GetGenericTypeDefinition() == typeof(Dictionary<,>); }
    public static bool Case2597() { return typeof(Dictionary<int, int>).GetGenericArguments()[0] == typeof(int); }
    public static bool Case2598() { return typeof(Dictionary<int, int>).GetGenericArguments()[1] == typeof(int); }
    public static bool Case2599() { return typeof(int?).IsGenericType; }
    public static bool Case2600() { return Nullable.GetUnderlyingType(typeof(int?)) == typeof(int); }
}
