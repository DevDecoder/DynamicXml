using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Creates an index for a <see cref="DynamicXObject">dynamic XObject</see> that will apply a XPath.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DXPath : IDynamicXFilter
{
    /// <summary>
    ///     The <see cref="System.Range"/> of elements to include.
    /// </summary>
    public readonly Range Range;

    /// <summary>
    ///     XPath to <see cref="System.Xml.XPath.Extensions.XPathEvaluate(XNode, string)">evaluate</see>.
    /// </summary>
    public readonly string XPath;

    /// <summary>
    ///     Creates new <see cref="DXPath" />
    /// </summary>
    /// <param name="xPath">The XPath.</param>
    public DXPath(string xPath)
    {
        XPath = xPath;
        Range = ..;
    }

    /// <summary>
    ///     Creates new <see cref="DXPath" />
    /// </summary>
    /// <param name="range">The <see cref="System.Range"/> of results to include.</param>
    /// <param name="xPath">The XPath.</param>
    public DXPath(string xPath, Range range)
    {
        XPath = xPath;
        Range = range;
    }

    /// <summary>
    ///     Selects child elements with the <paramref name="xPath">specified XPath</paramref>.
    /// </summary>
    /// <param name="xPath">The XPath.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXPath WithPath(string xPath)
    {
        return new DXPath(xPath);
    }

    /// <summary>
    ///     Selects child elements with the <paramref name="xPath">specified XPath</paramref>.
    /// </summary>
    /// <param name="xPath">The XPath.</param>
    /// <param name="range"> The <see cref="System.Range"/> of elements to include.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXPath WithPath(string xPath, Range range)
    {
        return new DXPath(xPath, range);
    }

    /// <summary>
    ///     Selects child elements with the <paramref name="xPath">specified XPath</paramref>.
    /// </summary>
    /// <param name="xPath">The XPath.</param>
    /// <param name="index">The <see cref="System.Index"/> of the element.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXPath WithPath(string xPath, Index index)
    {
        return new DXPath(xPath, index.ToRange());
    }

    /// <summary>
    ///     Selects child elements with the <paramref name="xPath">specified XPath</paramref>.
    /// </summary>
    /// <param name="xPath">The XPath.</param>
    /// <param name="index">The <see cref="System.Index"/> of the element.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DXPath WithPath(string xPath, int index)
    {
        var i = index < 0 ? Index.FromEnd(-index) : Index.FromStart(index);
        return new DXPath(xPath, i.ToRange());
    }

    #region Conversions

    /// <summary>
    ///     Convert from <see cref="string" /> to <see cref="DXPath" />.
    /// </summary>
    /// <param name="xPath">The XPath.</param>
    /// <returns>A <see cref="DXPath" /></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DXPath(string xPath)
    {
        return new DXPath(xPath);
    }

    #endregion

    /// <inheritdoc />
    public override string ToString() =>
        $"{Range} XPath: {XPath}";

    /// <inheritdoc />
    IEnumerable<object> IDynamicXFilter.Filter(IEnumerable<XObject> inputs, DynamicXOptions options)
    {
        var nodes = inputs.OfType<XNode>();

        // Add filter if necessary
        var xPath = XPath;
        if (string.IsNullOrWhiteSpace(xPath))
            // Filter to node types.
            return nodes;

        // We can compile our expression once.
        var expression = XPathExpression.Compile(xPath, options.NamespaceResolver);

        // We build our filter based on the expression's return type.
        var result = expression.ReturnType switch
        {
            XPathResultType.NodeSet => nodes.SelectMany(n =>
                n.CreateNavigator().Select(expression).OfType<XPathNavigator>().Select(n => n.UnderlyingObject)
                    .Cast<object>()),
            XPathResultType.Any => nodes.SelectMany(n => n.CreateNavigator().Evaluate(expression) switch
            {
                XPathNodeIterator i => i.OfType<XPathNavigator>().Select(n => n.UnderlyingObject).Cast<object>(),
                { } o => o.ToEnum()
            }),
            XPathResultType.Number or XPathResultType.String or XPathResultType.Boolean => nodes.Select(n =>
                n.CreateNavigator().Evaluate(expression)),
            _ => Array.Empty<object>()
        };

        // Add span if necessary
        if (!Equals(Range, Range.All))
        {
            result = result.Span(Range);
        }

        return result;
    }
}