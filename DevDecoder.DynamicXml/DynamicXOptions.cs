using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

public record class DynamicXOptions
{
    public static readonly DynamicXOptions Default = new();

    public ConvertMode ConvertMode = ConvertMode.ConvertOrDefault;
    public IndexResultIfNotFound IndexResultIfNotFound = IndexResultIfNotFound.Empty;
    public InvokeResultIfNotFound InvokeResultIfNotFound = InvokeResultIfNotFound.Empty;
    public PropertyResultIfNotFound PropertyResultIfNotFound = PropertyResultIfNotFound.Null;
    public string? BuiltInPrefix { get; init; } = "__";
    public string? AttributePrefix { get; init; } = string.Empty;
    public string? ElementPrefix { get; init; } = string.Empty;
    public string? EnumPrefix { get; init; } = string.Empty;

    public bool ExposeBuiltIns => BuiltInPrefix is not null;
    public bool ExposeAttributes => AttributePrefix is not null;
    public bool ExposeElement => ElementPrefix is not null;
    public bool ExposeEnums => EnumPrefix is not null;

    public Func<XNamespace, string, XName> GetXName { get; init; } = DefaultGetXName;
    public Func<XName, string> GetName { get; init; } = DefaultGetName;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XName DefaultGetXName(XNamespace @namespace, string name)
    {
        return @namespace + name;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DefaultGetName(XName name)
    {
        return name.LocalName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string? StripPrefix(string name, string? prefix)
    {
        return prefix is null
            ? null // Disabled
            : prefix.Length < 1
                ? name // No Prefix
                : name.StartsWith(prefix)
                    ? name.Substring(prefix.Length)
                    : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private XName? StripXPrefix(XNamespace @namespace, string name, string? prefix)
    {
        var result = StripPrefix(name, prefix);
        return result is null ? null : @namespace + result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetBuiltInName(string name)
    {
        return StripPrefix(name, BuiltInPrefix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal XName? GetAttributeXName(XNamespace @namespace, string name)
    {
        return StripXPrefix(@namespace, name, AttributePrefix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal XName? GetElementXName(XNamespace @namespace, string name)
    {
        return StripXPrefix(@namespace, name, ElementPrefix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal XName? GetAttributesXName(XNamespace @namespace, string name)
    {
        return StripXPrefix(@namespace, name,
            EnumPrefix is null || ElementPrefix is null ? null : EnumPrefix + ElementPrefix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal XName? GetElementsXName(XNamespace @namespace, string name)
    {
        return StripXPrefix(@namespace, name,
            EnumPrefix is null || ElementPrefix is null ? null : EnumPrefix + ElementPrefix);
    }
}

public enum ConvertMode
{
    ValueString,
    ConvertOrDefault,
    ConvertOrThrow
}

public enum InvokeResultIfNotFound
{
    Null,
    Empty,
    Throw
}

public enum PropertyResultIfNotFound
{
    Null,
    Throw
}

public enum IndexResultIfNotFound
{
    Empty,
    Throw
}