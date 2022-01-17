// ReSharper disable InconsistentNaming, MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will match any 
///     <see cref="XNode">nodes</see>.
/// </summary>
public readonly struct DXNode : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range"/> of nodes to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     Selects the first node.
    /// </summary>
    public static readonly DXNode First = new DXNode(0..1);

    /// <summary>
    ///     Selects the last node.
    /// </summary>
    public static readonly DXNode Last = new DXNode(^1..^0);

    /// <summary>
    ///     Creates new <see cref="DXNode" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range"/> of nodes to include.</param>
    public DXNode(Range range)
    {
        Range = range;
    }

    /// <summary>
    ///     Selects all nodes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXNode All()
    {
        return new DXNode(..);
    }

    /// <summary>
    ///     Selects all nodes in the <paramref name="range">specified range</paramref>.
    /// </summary>
    /// <param name="range"> The <see cref="System.Range"/> of nodes to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXNode Span(Range range)
    {
        return new DXNode(range);
    }

    /// <summary>
    ///     Selects the nodes at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the node. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXNode At(Index index)
    {
        return new DXNode(index.ToRange());
    }

    /// <summary>
    ///     Selects the nodes at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the node. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXNode At(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXNode(i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXNode" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXNode" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXNode(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXNode(i.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXNode" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXNode" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXNode(Index index)
    {
        return new DXNode(index.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Range"/> to <see cref="DXNode" />.
    /// </summary>
    /// <param name="range">The <see cref="Range"/></param>
    /// <returns>A <see cref="DXNode" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXNode(Range range)
    {
        return new DXNode(range);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"{Range} node";

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var result = inputs.OfType<XNode>();

        // Add span if necessary
        if (!Equals(Range, Range.All))
        {
            result = result.Span(Range);
        }

        return result;
    }
}

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will match any 
///     <see cref="XElement">elements</see>.
/// </summary>
public readonly struct DXElement : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range"/> of elements to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     Filters elements by their <see cref="XName.LocalName">local name</see>.
    /// </summary>
    /// <remarks>
    ///     NOTE that matching is controlled by <see cref="DynamicXOptions" />.
    /// </remarks>
    public readonly string? Name;

    /// <summary>
    ///     Selects the first element.
    /// </summary>
    public static readonly DXElement First = new DXElement(0..1);

    /// <summary>
    ///     Selects the last element.
    /// </summary>
    public static readonly DXElement Last = new DXElement(^1..^0);

    /// <summary>
    ///     Creates new <see cref="DXElement" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range"/> of elements to include.</param>
    public DXElement(Range range)
    {
        Name = null;
        Range = range;
    }

    /// <summary>
    ///     Creates new <see cref="DXElement" />
    /// </summary>
    /// <param name="name">Filters elements by their <see cref="XName.LocalName">local name</see>.</param>
    public DXElement(string name)
    {
        Name = name;
        Range = ..;
    }

    /// <summary>
    ///     Creates new <see cref="DXElement" />
    /// </summary>
    /// <param name="name">Filters elements by their <see cref="XName.LocalName">local name</see>.</param>
    /// <param name="range">The <see cref="System.Range"/> of elements to include.</param>
    public DXElement(string name, Range range)
    {
        Name = name;
        Range = range;
    }

    /// <summary>
    ///     Selects all elements.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXElement All()
    {
        return new DXElement(..);
    }

    /// <summary>
    ///     Selects all elements in the <paramref name="range">specified range</paramref>.
    /// </summary>
    /// <param name="range"> The <see cref="System.Range"/> of elements to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXElement Span(Range range)
    {
        return new DXElement(range);
    }

    /// <summary>
    ///     Selects the elements at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the element. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXElement At(Index index)
    {
        return new DXElement(index.ToRange());
    }

    /// <summary>
    ///     Selects the elements at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the element. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXElement At(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXElement(i.ToRange());
    }

    /// <summary>
    ///     Selects child elements with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">Filters elements by their <see cref="XName.LocalName">local name</see></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXElement WithName(string name)
    {
        return new DXElement(name);
    }

    /// <summary>
    ///     Selects child elements with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">Filters elements by their <see cref="XName.LocalName">local name</see></param>
    /// <param name="range"> The <see cref="System.Range"/> of elements to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXElement WithName(string name, Range range)
    {
        return new DXElement(name, range);
    }

    /// <summary>
    ///     Selects child elements with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">Filters elements by their <see cref="XName.LocalName">local name</see></param>
    /// <param name="index">The <see cref="System.Index"/> of the element.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXElement WithName(string name, Index index)
    {
        return new DXElement(name, index.ToRange());
    }

    /// <summary>
    ///     Selects child elements with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">Filters elements by their <see cref="XName.LocalName">local name</see></param>
    /// <param name="index">The <see cref="System.Index"/> of the element.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXElement WithName(string name, int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXElement(name, i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXElement" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXElement" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXElement(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXElement(i.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXElement" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXElement" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXElement(Index index)
    {
        return new DXElement(index.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Range"/> to <see cref="DXElement" />.
    /// </summary>
    /// <param name="range">The <see cref="Range"/></param>
    /// <returns>A <see cref="DXElement" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXElement(Range range)
    {
        return new DXElement(range);
    }

    /// <summary>
    ///     Convert from <see cref="string" /> to <see cref="DXElement" />.
    /// </summary>
    /// <param name="name">Filters elements by their <see cref="XName.LocalName">local name</see></param>
    /// <returns>A <see cref="DXElement" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXElement(string name)
    {
        return new DXElement(name);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"{Range} element{(string.IsNullOrWhiteSpace(Name) ? string.Empty : " ${Name}")}";

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var result = inputs.OfType<XElement>();

        // Add filter if necessary
        var name = Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var comparer = options.ElementComparer;
            result = result.Where(x => comparer.Equals(x.Name.LocalName, name));
        }

        // Add span if necessary
        if (!Equals(Range, Range.All))
        {
            result = result.Span(Range);
        }

        return result;
    }
}

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will match any 
///     <see cref="XAttribute">attributes</see>.
/// </summary>
public readonly struct DXAttribute : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range"/> of attributes to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     Filters attributes by their <see cref="XName.LocalName">local name</see>.
    /// </summary>
    /// <remarks>
    ///     NOTE that matching is controlled by <see cref="DynamicXOptions" />.
    /// </remarks>
    public readonly string? Name;

    /// <summary>
    ///     Selects the first attribute.
    /// </summary>
    public static readonly DXAttribute First = new DXAttribute(0..1);

    /// <summary>
    ///     Selects the last attribute.
    /// </summary>
    public static readonly DXAttribute Last = new DXAttribute(^1..^0);

    /// <summary>
    ///     Creates new <see cref="DXAttribute" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range"/> of attributes to include.</param>
    public DXAttribute(Range range)
    {
        Name = null;
        Range = range;
    }

    /// <summary>
    ///     Creates new <see cref="DXAttribute" />
    /// </summary>
    /// <param name="name">Filters attributes by their <see cref="XName.LocalName">local name</see>.</param>
    public DXAttribute(string name)
    {
        Name = name;
        Range = ..;
    }

    /// <summary>
    ///     Creates new <see cref="DXAttribute" />
    /// </summary>
    /// <param name="name">Filters attributes by their <see cref="XName.LocalName">local name</see>.</param>
    /// <param name="range">The <see cref="System.Range"/> of attributes to include.</param>
    public DXAttribute(string name, Range range)
    {
        Name = name;
        Range = range;
    }

    /// <summary>
    ///     Selects all attributes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXAttribute All()
    {
        return new DXAttribute(..);
    }

    /// <summary>
    ///     Selects all attributes in the <paramref name="range">specified range</paramref>.
    /// </summary>
    /// <param name="range"> The <see cref="System.Range"/> of attributes to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXAttribute Span(Range range)
    {
        return new DXAttribute(range);
    }

    /// <summary>
    ///     Selects the attributes at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the attribute. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXAttribute At(Index index)
    {
        return new DXAttribute(index.ToRange());
    }

    /// <summary>
    ///     Selects the attributes at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the attribute. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXAttribute At(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXAttribute(i.ToRange());
    }

    /// <summary>
    ///     Selects child attributes with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">Filters attributes by their <see cref="XName.LocalName">local name</see></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXAttribute WithName(string name)
    {
        return new DXAttribute(name);
    }

    /// <summary>
    ///     Selects child attributes with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">Filters attributes by their <see cref="XName.LocalName">local name</see></param>
    /// <param name="range"> The <see cref="System.Range"/> of attributes to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXAttribute WithName(string name, Range range)
    {
        return new DXAttribute(name, range);
    }

    /// <summary>
    ///     Selects child attributes with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">Filters attributes by their <see cref="XName.LocalName">local name</see></param>
    /// <param name="index">The <see cref="System.Index"/> of the attribute.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXAttribute WithName(string name, Index index)
    {
        return new DXAttribute(name, index.ToRange());
    }

    /// <summary>
    ///     Selects child attributes with the <paramref name="name">specified name</paramref>.
    /// </summary>
    /// <param name="name">Filters attributes by their <see cref="XName.LocalName">local name</see></param>
    /// <param name="index">The <see cref="System.Index"/> of the attribute.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXAttribute WithName(string name, int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXAttribute(name, i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXAttribute" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXAttribute" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXAttribute(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXAttribute(i.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXAttribute" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXAttribute" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXAttribute(Index index)
    {
        return new DXAttribute(index.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Range"/> to <see cref="DXAttribute" />.
    /// </summary>
    /// <param name="range">The <see cref="Range"/></param>
    /// <returns>A <see cref="DXAttribute" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXAttribute(Range range)
    {
        return new DXAttribute(range);
    }

    /// <summary>
    ///     Convert from <see cref="string" /> to <see cref="DXAttribute" />.
    /// </summary>
    /// <param name="name">Filters attributes by their <see cref="XName.LocalName">local name</see></param>
    /// <returns>A <see cref="DXAttribute" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXAttribute(string name)
    {
        return new DXAttribute(name);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"{Range} attribute{(string.IsNullOrWhiteSpace(Name) ? string.Empty : " ${Name}")}";

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var result = inputs.OfType<XAttribute>();

        // Add filter if necessary
        var name = Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var comparer = options.AttributeComparer;
            result = result.Where(x => comparer.Equals(x.Name.LocalName, name));
        }

        // Add span if necessary
        if (!Equals(Range, Range.All))
        {
            result = result.Span(Range);
        }

        return result;
    }
}

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will match any 
///     <see cref="XProcessingInstruction">processing instructions</see>.
/// </summary>
public readonly struct DXProcessingInstruction : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range"/> of processing instructions to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     Filters processing instructions by their <see cref="XProcessingInstruction.Target">target</see>.
    /// </summary>
    /// <remarks>
    ///     NOTE that matching is controlled by <see cref="DynamicXOptions" />.
    /// </remarks>
    public readonly string? Target;

    /// <summary>
    ///     Selects the first processing instruction.
    /// </summary>
    public static readonly DXProcessingInstruction First = new DXProcessingInstruction(0..1);

    /// <summary>
    ///     Selects the last processing instruction.
    /// </summary>
    public static readonly DXProcessingInstruction Last = new DXProcessingInstruction(^1..^0);

    /// <summary>
    ///     Creates new <see cref="DXProcessingInstruction" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range"/> of processing instructions to include.</param>
    public DXProcessingInstruction(Range range)
    {
        Target = null;
        Range = range;
    }

    /// <summary>
    ///     Creates new <see cref="DXProcessingInstruction" />
    /// </summary>
    /// <param name="target">Filters processing instructions by their <see cref="XProcessingInstruction.Target">target</see>.</param>
    public DXProcessingInstruction(string target)
    {
        Target = target;
        Range = ..;
    }

    /// <summary>
    ///     Creates new <see cref="DXProcessingInstruction" />
    /// </summary>
    /// <param name="target">Filters processing instructions by their <see cref="XProcessingInstruction.Target">target</see>.</param>
    /// <param name="range">The <see cref="System.Range"/> of processing instructions to include.</param>
    public DXProcessingInstruction(string target, Range range)
    {
        Target = target;
        Range = range;
    }

    /// <summary>
    ///     Selects all processing instructions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXProcessingInstruction All()
    {
        return new DXProcessingInstruction(..);
    }

    /// <summary>
    ///     Selects all processing instructions in the <paramref name="range">specified range</paramref>.
    /// </summary>
    /// <param name="range"> The <see cref="System.Range"/> of processing instructions to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXProcessingInstruction Span(Range range)
    {
        return new DXProcessingInstruction(range);
    }

    /// <summary>
    ///     Selects the processing instructions at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the processing instruction. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXProcessingInstruction At(Index index)
    {
        return new DXProcessingInstruction(index.ToRange());
    }

    /// <summary>
    ///     Selects the processing instructions at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the processing instruction. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXProcessingInstruction At(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXProcessingInstruction(i.ToRange());
    }

    /// <summary>
    ///     Selects child processing instructions with the <paramref name="target">specified target</paramref>.
    /// </summary>
    /// <param name="target">Filters processing instructions by their <see cref="XProcessingInstruction.Target">target</see></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXProcessingInstruction WithTarget(string target)
    {
        return new DXProcessingInstruction(target);
    }

    /// <summary>
    ///     Selects child processing instructions with the <paramref name="target">specified target</paramref>.
    /// </summary>
    /// <param name="target">Filters processing instructions by their <see cref="XProcessingInstruction.Target">target</see></param>
    /// <param name="range"> The <see cref="System.Range"/> of processing instructions to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXProcessingInstruction WithTarget(string target, Range range)
    {
        return new DXProcessingInstruction(target, range);
    }

    /// <summary>
    ///     Selects child processing instructions with the <paramref name="target">specified target</paramref>.
    /// </summary>
    /// <param name="target">Filters processing instructions by their <see cref="XProcessingInstruction.Target">target</see></param>
    /// <param name="index">The <see cref="System.Index"/> of the processing instruction.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXProcessingInstruction WithTarget(string target, Index index)
    {
        return new DXProcessingInstruction(target, index.ToRange());
    }

    /// <summary>
    ///     Selects child processing instructions with the <paramref name="target">specified target</paramref>.
    /// </summary>
    /// <param name="target">Filters processing instructions by their <see cref="XProcessingInstruction.Target">target</see></param>
    /// <param name="index">The <see cref="System.Index"/> of the processing instruction.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXProcessingInstruction WithTarget(string target, int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXProcessingInstruction(target, i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXProcessingInstruction" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXProcessingInstruction" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXProcessingInstruction(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXProcessingInstruction(i.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXProcessingInstruction" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXProcessingInstruction" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXProcessingInstruction(Index index)
    {
        return new DXProcessingInstruction(index.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Range"/> to <see cref="DXProcessingInstruction" />.
    /// </summary>
    /// <param name="range">The <see cref="Range"/></param>
    /// <returns>A <see cref="DXProcessingInstruction" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXProcessingInstruction(Range range)
    {
        return new DXProcessingInstruction(range);
    }

    /// <summary>
    ///     Convert from <see cref="string" /> to <see cref="DXProcessingInstruction" />.
    /// </summary>
    /// <param name="target">Filters processing instructions by their <see cref="XProcessingInstruction.Target">target</see></param>
    /// <returns>A <see cref="DXProcessingInstruction" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXProcessingInstruction(string target)
    {
        return new DXProcessingInstruction(target);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"{Range} processing instruction{(string.IsNullOrWhiteSpace(Target) ? string.Empty : " ${Target}")}";

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var result = inputs.OfType<XProcessingInstruction>();

        // Add filter if necessary
        var target = Target;
        if (!string.IsNullOrWhiteSpace(target))
        {
            var comparer = options.TargetComparer;
            result = result.Where(x => comparer.Equals(x.Target, target));
        }

        // Add span if necessary
        if (!Equals(Range, Range.All))
        {
            result = result.Span(Range);
        }

        return result;
    }
}

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will match any 
///     <see cref="XComment">comments</see>.
/// </summary>
public readonly struct DXComment : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range"/> of comments to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     Selects the first comment.
    /// </summary>
    public static readonly DXComment First = new DXComment(0..1);

    /// <summary>
    ///     Selects the last comment.
    /// </summary>
    public static readonly DXComment Last = new DXComment(^1..^0);

    /// <summary>
    ///     Creates new <see cref="DXComment" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range"/> of comments to include.</param>
    public DXComment(Range range)
    {
        Range = range;
    }

    /// <summary>
    ///     Selects all comments.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXComment All()
    {
        return new DXComment(..);
    }

    /// <summary>
    ///     Selects all comments in the <paramref name="range">specified range</paramref>.
    /// </summary>
    /// <param name="range"> The <see cref="System.Range"/> of comments to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXComment Span(Range range)
    {
        return new DXComment(range);
    }

    /// <summary>
    ///     Selects the comments at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the comment. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXComment At(Index index)
    {
        return new DXComment(index.ToRange());
    }

    /// <summary>
    ///     Selects the comments at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the comment. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXComment At(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXComment(i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXComment" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXComment" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXComment(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXComment(i.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXComment" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXComment" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXComment(Index index)
    {
        return new DXComment(index.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Range"/> to <see cref="DXComment" />.
    /// </summary>
    /// <param name="range">The <see cref="Range"/></param>
    /// <returns>A <see cref="DXComment" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXComment(Range range)
    {
        return new DXComment(range);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"{Range} comment";

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var result = inputs.OfType<XComment>();

        // Add span if necessary
        if (!Equals(Range, Range.All))
        {
            result = result.Span(Range);
        }

        return result;
    }
}

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will match any 
///     <see cref="XText">text blocks</see>.
/// </summary>
public readonly struct DXText : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range"/> of text blocks to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     Selects the first text block.
    /// </summary>
    public static readonly DXText First = new DXText(0..1);

    /// <summary>
    ///     Selects the last text block.
    /// </summary>
    public static readonly DXText Last = new DXText(^1..^0);

    /// <summary>
    ///     Creates new <see cref="DXText" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range"/> of text blocks to include.</param>
    public DXText(Range range)
    {
        Range = range;
    }

    /// <summary>
    ///     Selects all text blocks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXText All()
    {
        return new DXText(..);
    }

    /// <summary>
    ///     Selects all text blocks in the <paramref name="range">specified range</paramref>.
    /// </summary>
    /// <param name="range"> The <see cref="System.Range"/> of text blocks to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXText Span(Range range)
    {
        return new DXText(range);
    }

    /// <summary>
    ///     Selects the text blocks at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the text block. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXText At(Index index)
    {
        return new DXText(index.ToRange());
    }

    /// <summary>
    ///     Selects the text blocks at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the text block. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXText At(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXText(i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXText" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXText" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXText(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXText(i.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXText" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXText" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXText(Index index)
    {
        return new DXText(index.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Range"/> to <see cref="DXText" />.
    /// </summary>
    /// <param name="range">The <see cref="Range"/></param>
    /// <returns>A <see cref="DXText" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXText(Range range)
    {
        return new DXText(range);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"{Range} text block";

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var result = inputs.OfType<XText>();

        // Add span if necessary
        if (!Equals(Range, Range.All))
        {
            result = result.Span(Range);
        }

        return result;
    }
}

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will match any 
///     <see cref="XCData">character data blocks</see>.
/// </summary>
public readonly struct DXCData : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range"/> of character data blocks to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     Selects the first character data block.
    /// </summary>
    public static readonly DXCData First = new DXCData(0..1);

    /// <summary>
    ///     Selects the last character data block.
    /// </summary>
    public static readonly DXCData Last = new DXCData(^1..^0);

    /// <summary>
    ///     Creates new <see cref="DXCData" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range"/> of character data blocks to include.</param>
    public DXCData(Range range)
    {
        Range = range;
    }

    /// <summary>
    ///     Selects all character data blocks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXCData All()
    {
        return new DXCData(..);
    }

    /// <summary>
    ///     Selects all character data blocks in the <paramref name="range">specified range</paramref>.
    /// </summary>
    /// <param name="range"> The <see cref="System.Range"/> of character data blocks to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXCData Span(Range range)
    {
        return new DXCData(range);
    }

    /// <summary>
    ///     Selects the character data blocks at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the character data block. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXCData At(Index index)
    {
        return new DXCData(index.ToRange());
    }

    /// <summary>
    ///     Selects the character data blocks at the <paramref name="index">specified index</paramref>.
    /// </summary>
    /// <param name="index">The <see cref="System.Index"/> of the character data block. 
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXCData At(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXCData(i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXCData" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXCData" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXCData(int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXCData(i.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Index"/> to <see cref="DXCData" />.
    /// </summary>
    /// <param name="index">The <see cref="Index"/></param>
    /// <returns>A <see cref="DXCData" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXCData(Index index)
    {
        return new DXCData(index.ToRange());
    }

    /// <summary>
    ///     Convert from <see cref="Range"/> to <see cref="DXCData" />.
    /// </summary>
    /// <param name="range">The <see cref="Range"/></param>
    /// <returns>A <see cref="DXCData" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXCData(Range range)
    {
        return new DXCData(range);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"{Range} character data block";

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var result = inputs.OfType<XCData>();

        // Add span if necessary
        if (!Equals(Range, Range.All))
        {
            result = result.Span(Range);
        }

        return result;
    }
}