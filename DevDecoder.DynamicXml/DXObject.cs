using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will match any
///     <see cref="XObject">objects</see>.
/// </summary>
// ReSharper disable once InconsistentNaming
public readonly struct DXObject : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range" /> of objects to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     Filters objects by their <see cref="XName.LocalName">local name</see>, if an attribute or element, or by their
    ///     <see cref="XProcessingInstruction.Target">target</see> if a processing instruction..
    /// </summary>
    /// <remarks>
    ///     NOTE that matching is controlled by <see cref="DynamicXOptions" />.
    /// </remarks>
    public readonly string? Name;

    /// <summary>
    ///     Selects the first object.
    /// </summary>
    public static readonly DXObject First = new(..1);

    /// <summary>
    ///     Selects the last object.
    /// </summary>
    public static readonly DXObject Last = new(^1..);

    /// <summary>
    ///     Creates new <see cref="DXObject" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range" /> of objects to include.</param>
    public DXObject(Range range)
    {
        Name = null;
        Range = range;
    }

    /// <summary>
    ///     Creates new <see cref="DXObject" />
    /// </summary>
    /// <param name="name">
    ///     Filters objects by their <see cref="XName.LocalName">local name</see>, if an attribute or element,
    ///     or by their <see cref="XProcessingInstruction.Target">target</see> if a processing instruction..
    /// </param>
    public DXObject(string name)
    {
        Name = name;
        Range = ..;
    }

    /// <summary>
    ///     Creates new <see cref="DXObject" />
    /// </summary>
    /// <param name="name">
    ///     Filters objects by their <see cref="XName.LocalName">local name</see>, if an attribute or element,
    ///     or by their <see cref="XProcessingInstruction.Target">target</see> if a processing instruction..
    /// </param>
    /// <param name="range">The <see cref="System.Range" /> of objects to include.</param>
    public DXObject(string name, Range range)
    {
        Name = name;
        Range = range;
    }

    /// <summary>
    ///     Selects all objects.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXObject All()
    {
        return new DXObject(..);
    }

    /// <summary>
    ///     Selects all objects in the <paramref name="range">specified range</paramref>.
    /// </summary>
    /// <param name="range"> The <see cref="System.Range" /> of objects to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXObject Span(Range range)
    {
        return new DXObject(range);
    }

    /// <summary>
    ///     Selects the objects at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">
    ///     The <see cref="System.Index" /> of the object.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXObject At(Index index)
    {
        return new DXObject(index.ToRange());
    }

    /// <summary>
    ///     Selects the objects at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">
    ///     The <see cref="System.Index" /> of the object.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXObject At(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXObject(i.ToRange());
    }

    /// <summary>
    ///     Selects child objects with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">
    ///     Filters objects by their <see cref="XName.LocalName">local name</see>, if an attribute or element,
    ///     or by their <see cref="XProcessingInstruction.Target">target</see> if a processing instruction.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXObject WithName(string name)
    {
        return new DXObject(name);
    }

    /// <summary>
    ///     Selects child objects with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">
    ///     Filters objects by their <see cref="XName.LocalName">local name</see>, if an attribute or element,
    ///     or by their <see cref="XProcessingInstruction.Target">target</see> if a processing instruction.
    /// </param>
    /// <param name="range"> The <see cref="System.Range" /> of objects to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXObject WithName(string name, Range range)
    {
        return new DXObject(name, range);
    }

    /// <summary>
    ///     Selects child objects with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">
    ///     Filters objects by their <see cref="XName.LocalName">local name</see>, if an attribute or element,
    ///     or by their <see cref="XProcessingInstruction.Target">target</see> if a processing instruction.
    /// </param>
    /// <param name="index">The <see cref="System.Index" /> of the object.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXObject WithName(string name, Index index)
    {
        return new DXObject(name, index.ToRange());
    }

    /// <summary>
    ///     Selects child objects with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">
    ///     Filters objects by their <see cref="XName.LocalName">local name</see>, if an attribute or element,
    ///     or by their <see cref="XProcessingInstruction.Target">target</see> if a processing instruction.
    /// </param>
    /// <param name="index">The <see cref="System.Index" /> of the object.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXObject WithName(string name, int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXObject(name, i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="Index" /> to <see cref="DXObject" />.
    /// </summary>
    /// <param name="index">The <see cref="Index" /></param>
    /// <returns>A <see cref="DXObject" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXObject(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXObject(i.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Index" /> to <see cref="DXObject" />.
    /// </summary>
    /// <param name="index">The <see cref="Index" /></param>
    /// <returns>A <see cref="DXObject" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXObject(Index index)
    {
        return new DXObject(index.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Range" /> to <see cref="DXObject" />.
    /// </summary>
    /// <param name="range">The <see cref="Range" /></param>
    /// <returns>A <see cref="DXObject" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXObject(Range range)
    {
        return new DXObject(range);
    }

    /// <summary>
    ///     Convert from <see cref="string" /> to <see cref="DXObject" />.
    /// </summary>
    /// <param name="name">
    ///     Filters objects by their <see cref="XName.LocalName">local name</see>, if an attribute or element,
    ///     or by their <see cref="XProcessingInstruction.Target">target</see> if a processing instruction.
    /// </param>
    /// <returns>A <see cref="DXObject" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXObject(string name)
    {
        return new DXObject(name);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Range} object{(string.IsNullOrWhiteSpace(Name) ? string.Empty : " ${Name}")}";
    }

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var result = inputs;

        // Add filter if necessary
        var name = Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var attributeComparer = options.AttributeComparer;
            var elementComparer = options.ElementComparer;
            var targetComparer = options.TargetComparer;
            result = result.Where(x => x switch
            {
                XAttribute xAttribute => attributeComparer.Equals(xAttribute.Name.LocalName, name),
                XElement xElement => elementComparer.Equals(xElement.Name.LocalName, name),
                XProcessingInstruction xProcessingInstruction => targetComparer.Equals(xProcessingInstruction.Target,
                    name),
                _ => false
            });
        }

        // Add span if necessary
        if (!Equals(Range, Range.All)) result = result.Span(Range);

        return result;
    }
}