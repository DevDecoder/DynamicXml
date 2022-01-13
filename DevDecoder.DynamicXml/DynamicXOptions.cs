using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

public record class DynamicXOptions
{
    /// <summary>
    ///     The default options.
    /// </summary>
    public static readonly DynamicXOptions Default = new();

    /// <summary>
    ///     Controls conversion of dynamic objects.
    /// </summary>
    public ConvertMode ConvertMode = ConvertMode.Default;

    /// <summary>
    ///     Controls what to do when an index is not found.
    /// </summary>
    public IndexResultIfNotFound IndexResultIfNotFound = IndexResultIfNotFound.Default;

    /// <summary>
    ///     Controls what to do when a method is not found.
    /// </summary>
    public InvokeResultIfNotFound InvokeResultIfNotFound = InvokeResultIfNotFound.Default;

    /// <summary>
    ///     Controls what to do when a property is not found.
    /// </summary>
    public PropertyResultIfNotFound PropertyResultIfNotFound = PropertyResultIfNotFound.Default;

    /// <summary>
    ///     Optional prefix for disambiguating built-in properties/methods; or <see langword="null" /> to disable
    ///     matching built-ins.
    /// </summary>
    public string? BuiltInPrefix { get; init; } = string.Empty;

    /// <summary>
    ///     Optional prefix for disambiguating attributes; or <see langword="null" /> to disable
    ///     matching attributes.
    /// </summary>
    public string? AttributePrefix { get; init; } = string.Empty;

    /// <summary>
    ///     Optional prefix for disambiguating elements; or <see langword="null" /> to disable
    ///     matching elements.
    /// </summary>
    public string? ElementPrefix { get; init; } = string.Empty;

    /// <summary>
    ///     Optional prefix for disambiguating enumeration methods; or <see langword="null" /> to disable
    ///     matching enumeration methods.
    /// </summary>
    /// <remarks>
    ///     When looking for an enumeration this prefix is also prefixed to the appropriate <see cref="AttributePrefix" /> or
    ///     <see cref="ElementPrefix" />.  For example, if set to `e` and, <see cref="AttributePrefix" /> is set to `a`, then
    ///     calling <code>node.eaChild()</code> will return all attributes called `Child`, or, if that fails, all child
    ///     elements called
    ///     `aChild`.  Finally, it will look for a method on the `XObject` called `eaChild()`.
    /// </remarks>
    public string? EnumPrefix { get; init; } = string.Empty;

    /// <summary>
    ///     <see langword="true" /> if built-in methods and properties are exposed; otherwise, <see langword="false" />.
    /// </summary>
    public bool ExposeBuiltIns => BuiltInPrefix is not null;

    /// <summary>
    ///     <see langword="true" /> if attributes are exposed; otherwise, <see langword="false" />.
    /// </summary>
    public bool ExposeAttributes => AttributePrefix is not null;

    /// <summary>
    ///     <see langword="true" /> if elements are exposed; otherwise, <see langword="false" />.
    /// </summary>
    public bool ExposeElement => ElementPrefix is not null;

    /// <summary>
    ///     <see langword="true" /> if enumerations are exposed; otherwise, <see langword="false" />.
    /// </summary>
    public bool ExposeEnums => EnumPrefix is not null;

    /// <summary>
    ///     Optional <see cref="StringComparer" /> that determines how an element's <see cref="XName.LocalName" /> is matched
    ///     with the requested dynamic member name.
    /// </summary>
    public StringComparer ElementComparer { get; init; } = StringComparer.Ordinal;

    /// <summary>
    ///     Optional <see cref="StringComparer" /> that determines how an attribute's <see cref="XName.LocalName" /> is matched
    ///     with the requested dynamic member name.
    /// </summary>
    public StringComparer AttributeComparer { get; init; } = StringComparer.Ordinal;

    /// <summary>
    ///     Optional function that allows the mapping of a requested dynamic member name into the equivalent
    ///     <see cref="XName.LocalName" /> for an attribute.
    /// </summary>
    public Func<string?, string?> MapToAttributeName { get; init; } = DefaultMapToName;

    /// <summary>
    ///     Optional function that allows the mapping of a requested dynamic member name into the equivalent
    ///     <see cref="XName.LocalName" /> for an attribute.
    /// </summary>
    public Func<string?, string?> MapToElementName { get; init; } = DefaultMapToName;

    /// <summary>
    ///     Optional function that allows the mapping of a requested dynamic member name into the equivalent name for a
    ///     built-in member.
    /// </summary>
    public Func<string?, string?> MapToBuiltInName { get; init; } = DefaultMapToName;

    /// <summary>
    ///     The default mapper passes the <paramref name="name" /> straight through.
    /// </summary>
    /// <param name="name">The dynamic member name requested.</param>
    /// <returns>The mapped name.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? DefaultMapToName(string? name)
    {
        return name;
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
        return MapToBuiltInName(StripPrefix(name, BuiltInPrefix));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetAttributeName(string name)
    {
        return MapToAttributeName(StripPrefix(name, AttributePrefix));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetElementName(string name)
    {
        return MapToElementName(StripPrefix(name, ElementPrefix));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetAttributesName(string name)
    {
        return MapToAttributeName(StripPrefix(name,
            EnumPrefix is null || ElementPrefix is null ? null : EnumPrefix + ElementPrefix));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string? GetElementsName(string name)
    {
        return MapToElementName(StripPrefix(name,
            EnumPrefix is null || ElementPrefix is null ? null : EnumPrefix + ElementPrefix));
    }
}