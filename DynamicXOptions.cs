using System;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

public record class DynamicXOptions
{
    public string? BuiltInPrefix { get; init; } = "__";
    public string? AttributePrefix { get; init; } = string.Empty;
    public string? ElementPrefix { get; init; } = string.Empty;
    public string? EnumPrefix { get; init; } = string.Empty;

    public bool ThrowInvalidCast = false;

    public bool ThrowIfNotFound = false;

    public bool ExposeBuiltIns => BuiltInPrefix is not null;
    public bool ExposeAttributes => AttributePrefix is not null;
    public bool ExposeElement => ElementPrefix is not null;
    public bool ExposeEnums => EnumPrefix is not null;

    public Func<XNamespace, string, XName> GetXName { get; init; } = DefaultGetXName;
    public Func<XName, string> GetName { get; init; } = DefaultGetName;
    
    public static readonly DynamicXOptions Default = new DynamicXOptions();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XName DefaultGetXName(XNamespace @namespace, string name) => @namespace + name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DefaultGetName(XName name) => name.LocalName;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string? StripPrefix(string name, string? prefix) =>
        prefix is null
            ? null // Disabled
            : prefix.Length < 1
                ? name // No Prefix
                : name.StartsWith(prefix)
                    ? name.Substring(prefix.Length)
                    : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private XName? StripXPrefix(XNamespace @namespace, string name, string? prefix)
    {
        var result = StripPrefix(name, prefix);
        return result is null ? null : @namespace + result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetBuiltInName(string name) => StripPrefix(name, BuiltInPrefix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal XName? GetAttributeXName(XNamespace @namespace, string name) =>
        StripXPrefix(@namespace, name, AttributePrefix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal XName? GetElementXName(XNamespace @namespace, string name) =>
        StripXPrefix(@namespace, name, ElementPrefix);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal XName? GetAttributesXName(XNamespace @namespace, string name) =>
        StripXPrefix(@namespace, name, EnumPrefix is null || ElementPrefix is null ? null : EnumPrefix + ElementPrefix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal XName? GetElementsXName(XNamespace @namespace, string name) =>
        StripXPrefix(@namespace, name, EnumPrefix is null || ElementPrefix is null ? null : EnumPrefix + ElementPrefix);

    [Flags]
    internal enum NameType
    {
        BuiltIn,
        Attribute,
        Element,
        Elements
    }
}