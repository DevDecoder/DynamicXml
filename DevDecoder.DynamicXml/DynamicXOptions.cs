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

    public string? BuiltInPrefix { get; init; } = string.Empty;
    public string? AttributePrefix { get; init; } = string.Empty;
    public string? ElementPrefix { get; init; } = string.Empty;
    public string? EnumPrefix { get; init; } = string.Empty;

    public bool ExposeBuiltIns => BuiltInPrefix is not null;
    public bool ExposeAttributes => AttributePrefix is not null;
    public bool ExposeElement => ElementPrefix is not null;
    public bool ExposeEnums => EnumPrefix is not null;

    public StringComparer ElementComparer => StringComparer.Ordinal;
    public StringComparer AttributeComparer => StringComparer.Ordinal;

    public Func<string?, string?> GetXName { get; init; } = DefaultGetXName;
    public Func<XName, string> GetName { get; init; } = DefaultGetName;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? DefaultGetXName(string? name)
    {
        return name;
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
    internal string? GetBuiltInName(string name)
    {
        return GetXName(StripPrefix(name, BuiltInPrefix));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetAttributeName(string name)
    {
        return GetXName(StripPrefix(name, AttributePrefix));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetElementName(string name)
    {
        return GetXName(StripPrefix(name, ElementPrefix));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetAttributesName(string name)
    {
        return GetXName(StripPrefix(name,
            EnumPrefix is null || ElementPrefix is null ? null : EnumPrefix + ElementPrefix));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetElementsName(string name)
    {
        return GetXName(StripPrefix(name,
            EnumPrefix is null || ElementPrefix is null ? null : EnumPrefix + ElementPrefix));
    }
}