using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Builtin implementations of <see cref="IDynamicXFilter"/>, alongside a convenience class for implementing <see cref="IDynamicXFilter"/>
///     using a <see cref="Func{T1, T2, TResult}" >simple function</see>.  Can be used to as index.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class DXFilter : IDynamicXFilter
{
    /// <summary>
    ///     Creates a <see cref="DXFilter"/> that filters <see cref="XObject">XObjects</see> 
    /// </summary>
    /// <param name="filter">The filter</param>
    public static DXFilter Where(Func<XObject, bool> filter)
        => new DXFilter((inputs, _) => inputs.Where(filter));

    /// <summary>
    ///     Creates a <see cref="DXFilter"/> that filters <see cref="XObject">XObjects</see> 
    /// </summary>
    /// <param name="filter">The filter</param>
    public static DXFilter Where(Func<XObject, int, bool> filter)
        => new DXFilter((inputs, _) => inputs.Where(filter));

    /// <summary>
    ///     Creates a <see cref="DXFilter"/> that filters <see cref="XObject">XObjects</see> 
    /// </summary>
    /// <param name="filter">The filter</param>
    public static DXFilter Where(Func<XObject, DynamicXOptions, bool> filter)
        => new DXFilter((inputs, options) => inputs.Where(x => filter(x, options)));

    /// <summary>
    ///     Creates a <see cref="DXFilter"/> that filters <see cref="XObject">XObjects</see> 
    /// </summary>
    /// <param name="filter">The filter</param>
    public static DXFilter Where(Func<XObject, DynamicXOptions, int, bool> filter)
        => new DXFilter((inputs, options) => inputs.Where((x, i) => filter(x, options, i)));

    /// <summary>
    ///     Selects the owning <see cref="XDocument"/>.
    /// </summary>
    public static readonly DXFilter Document = new DXFilter(
        (inputs, _) => inputs.Where(o => o.Document is not null).Select(o => o.Document!).Distinct());

    /// <summary>
    ///     Selects the owning <see cref="XDocument.Root">root</see> <see cref="XElement"/> if any.
    /// </summary>
    public static readonly DXFilter Root = new DXFilter(
        (inputs, _) => inputs.Where(o => o.Document?.Root is not null).Select(o => o.Document?.Root!).Distinct());

    /// <summary>
    ///     Selects all distinct <see cref="XObject"/>.
    /// </summary>
    public static readonly DXFilter Distinct = new DXFilter(
        (inputs, _) => inputs.Distinct());

    /// <summary>
    ///     Selects all attributes.
    /// </summary>
    public static readonly DXFilter Attributes = new DXFilter(
        (inputs, _) => inputs.OfType<XElement>().SelectMany(e => e.Attributes()));

    /// <summary>
    ///     Selects all ancestor elements, starting with <see cref="Document"/>, and ending with <see cref="Parent"/>.
    /// </summary>
    public static readonly DXFilter Ancestors = new DXFilter(
        (inputs, _) => inputs.OfType<XNode>().SelectMany(n => n.Ancestors()));

    /// <summary>
    ///     Selects all ancestor elements, starting with <see cref="Document"/>, and ending with self.
    /// </summary>
    public static readonly DXFilter AncestorsAndSelf = new DXFilter(
        (inputs, _) => inputs.OfType<XNode>().SelectMany(n => n.Ancestors().Concat(n.ToEnum())));

    /// <summary>
    ///     Selects all children.
    /// </summary>
    public static readonly DXFilter Children = new DXFilter(
        (inputs, _) => inputs.OfType<XNode>().SelectMany(
            n => n is XElement e
                ? e.Nodes()
                : n is XDocument doc
                    ? doc.Root.ToEnum<object>()
                    : Array.Empty<XObject>()));

    /// <summary>
    ///     Selects all descendants.
    /// </summary>
    public static readonly DXFilter Descendants = new DXFilter(
        (inputs, _) => inputs.OfType<XNode>().SelectMany(
            n => n switch
            {
                XElement e => e.Descendants(),
                XDocument doc => doc.Descendants(),
                _ => Array.Empty<XElement>()
            }));

    /// <summary>
    ///     Selects self and then all descendants.
    /// </summary>
    public static readonly DXFilter DescendantsAndSelf = new DXFilter(
        (inputs, _) => inputs.OfType<XNode>().SelectMany(
            n => n switch
            {
                XElement e => e.DescendantsAndSelf(),
                XDocument doc => doc.Descendants().Concat(doc.ToEnum<object>()),
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
    public static readonly DXFilter FollowingSiblings = new DXFilter(
        (inputs, _) => inputs.OfType<XNode>().SelectMany(n => n.NodesAfterSelf()));

    /// <summary>
    ///     Selects all preceding siblings.
    /// </summary>
    public static readonly DXFilter PrecedingSiblings = new DXFilter(
        (inputs, _) => inputs.OfType<XNode>().SelectMany(n => n.NodesBeforeSelf()));

    /// <summary>
    ///     Selects parent, if any.
    /// </summary>
    public static readonly DXFilter Parent = new DXFilter(
        (inputs, _) => inputs.SelectMany(o => o.Parent.ToEnum()));

    /// <summary>
    /// Holds the custom filter function.
    /// </summary>
    private readonly Func<IEnumerable<XObject>, DynamicXOptions, IEnumerable<object>> _filter;

    /// <summary>
    /// Creates new instance of <see cref="DXFilter"/>
    /// </summary>
    /// <param name="filter">The custom filter.</param>
    public DXFilter(Func<IEnumerable<XObject>, DynamicXOptions, IEnumerable<object>> filter)
    {
        _filter = filter;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
        => _filter(inputs, options);
}