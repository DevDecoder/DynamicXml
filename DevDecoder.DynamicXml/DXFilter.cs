using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Builtin implementations of <see cref="IDynamicXFilter" />, alongside a convenience class for implementing
///     <see cref="IDynamicXFilter" />
///     using a <see cref="Func{T1,T2,TResult}">simple function</see>.  Can be used to as index.
/// </summary>
// ReSharper disable once InconsistentNaming
public readonly struct DXFilter : IDynamicXFilter
{
    /// <summary>
    ///     Creates a <see cref="DXFilter" /> that filters <see cref="XObject">XObjects</see>
    /// </summary>
    /// <param name="filter">The filter</param>
    public static DXFilter Where(Func<XObject, bool> filter)
    {
        return new DXFilter(nameof(Where), (inputs, _) => inputs.Where(filter));
    }

    /// <summary>
    ///     Creates a <see cref="DXFilter" /> that filters <see cref="XObject">XObjects</see>
    /// </summary>
    /// <param name="filter">The filter</param>
    public static DXFilter Where(Func<XObject, int, bool> filter)
    {
        return new DXFilter(nameof(Where), (inputs, _) => inputs.Where(filter));
    }

    /// <summary>
    ///     Creates a <see cref="DXFilter" /> that filters <see cref="XObject">XObjects</see>
    /// </summary>
    /// <param name="filter">The filter</param>
    public static DXFilter Where(Func<XObject, DynamicXOptions, bool> filter)
    {
        return new DXFilter(nameof(Where), (inputs, options) => inputs.Where(x => filter(x, options)));
    }

    /// <summary>
    ///     Creates a <see cref="DXFilter" /> that filters <see cref="XObject">XObjects</see>
    /// </summary>
    /// <param name="filter">The filter</param>
    public static DXFilter Where(Func<XObject, DynamicXOptions, int, bool> filter)
    {
        return new DXFilter(nameof(Where), (inputs, options) => inputs.Where((x, i) => filter(x, options, i)));
    }

    /// <summary>
    ///     Selects the owning <see cref="XDocument" />.
    /// </summary>
    public static readonly DXFilter Document = new(nameof(Document),
        (inputs, _) => inputs.Select(o => o.Document).Where(o => o is not null).Distinct()!);

    /// <summary>
    ///     Selects the owning <see cref="XDocument.Root">root</see> <see cref="XElement" /> if any.
    /// </summary>
    public static readonly DXFilter Root = new(nameof(Root),
        (inputs, _) => inputs.Select(o => o.Document?.Root).Where(o => o is not null).Distinct()!);

    /// <summary>
    ///     Selects all distinct <see cref="XObject" />.
    /// </summary>
    public static readonly DXFilter Distinct = new(nameof(Distinct), (inputs, _) => inputs.Distinct());

    /// <summary>
    ///     Selects all attributes.
    /// </summary>
    public static readonly DXFilter Attributes =
        new(nameof(Attributes), (inputs, _) => inputs.OfType<XElement>().SelectMany(e => e.Attributes()));

    /// <summary>
    ///     Selects all ancestor elements, starting with <see cref="Document" />, and ending with <see cref="Parent" />.
    /// </summary>
    public static readonly DXFilter Ancestors = new(nameof(Ancestors),
        (inputs, _) => inputs.OfType<XNode>().SelectMany(n => n.Ancestors()));

    /// <summary>
    ///     Selects all ancestor elements, starting with <see cref="Document" />, and ending with self.
    /// </summary>
    public static readonly DXFilter AncestorsAndSelf = new(nameof(AncestorsAndSelf),
        (inputs, _) => inputs.OfType<XNode>().SelectMany(n => n.Ancestors().Concat(n.ToEnum())));

    /// <summary>
    ///     Selects all child nodes (i.e. all child <see cref="XObject">XObjects</see>, except
    ///     <see cref="XAttribute">XAttributes</see>).
    /// </summary>
    public static readonly DXFilter Children = new(nameof(Children),
        (inputs, _) => inputs.OfType<XContainer>().SelectMany(c => c.Nodes()));

    /// <summary>
    ///     Selects all descendants.
    /// </summary>
    public static readonly DXFilter Descendants = new(nameof(Descendants),
        (inputs, _) => inputs.OfType<XContainer>().SelectMany(c => c.DescendantNodes()));

    /// <summary>
    ///     Selects self and then all descendants.
    /// </summary>
    public static readonly DXFilter DescendantsAndSelf = new(nameof(DescendantsAndSelf), (inputs, _) => inputs
        .OfType<XContainer>()
        .SelectMany(
            n => n switch
            {
                XElement e => e.DescendantNodesAndSelf(),
                XDocument doc => doc.DescendantNodes().Concat(doc.ToEnum<object>()),
                _ => Array.Empty<XObject>()
            }));

#if false // TODO
    /// <summary>
    ///     Selects all following.
    /// </summary>
    public static readonly DXFilter Following = new DXFilter(
        (inputs, options) => inputs.OfType<XNode>().SelectMany(n => n.NodesAfterSelf()));

    /// <summary>
    ///     Selects all preceding.
    /// </summary>
    public static readonly DXFilter Preceding = new DXFilter(
        (inputs, options) => inputs.OfType<XNode>().SelectMany(n => n.NodesBeforeSelf()));
#endif

    /// <summary>
    ///     Selects all following siblings.
    /// </summary>
    public static readonly DXFilter FollowingSiblings =
        new(nameof(FollowingSiblings), (inputs, _) => inputs.OfType<XNode>().SelectMany(n => n.NodesAfterSelf()));

    /// <summary>
    ///     Selects all preceding siblings.
    /// </summary>
    public static readonly DXFilter PrecedingSiblings =
        new(nameof(PrecedingSiblings), (inputs, _) => inputs.OfType<XNode>().SelectMany(n => n.NodesBeforeSelf()));

    /// <summary>
    ///     Selects parent, if any.
    /// </summary>
    public static readonly DXFilter Parent = new(nameof(Parent),
        (inputs, _) => inputs.SelectMany(o => o.Parent.ToEnum()));

    /// <summary>
    /// A friendly name for the filter.
    /// </summary>
    public readonly string Name;

    /// <summary>
    ///     Holds the custom filter function.
    /// </summary>
    private readonly Func<IEnumerable<XObject>, DynamicXOptions, IEnumerable<object>> _filter;

    /// <summary>
    ///     Creates new instance of <see cref="DXFilter" />
    /// </summary>
    /// <param name="name"></param>
    /// <param name="filter">The custom filter.</param>
    public DXFilter(string name, Func<IEnumerable<XObject>, DynamicXOptions, IEnumerable<object>> filter)
    {
        Name = name;
        _filter = filter;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        return _filter(inputs, options);
    }

    public override string ToString() => $"{Name} filter";
}