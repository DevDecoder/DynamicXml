using System.Collections.Generic;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
/// Interface for filters, can be supplied as index.
/// </summary>
public interface IDynamicXFilter
{
    /// <summary>
    /// Lazily filters the <paramref name="inputs">input</paramref> <see cref="XObject">XObjects</see>.
    /// </summary>
    /// <param name="inputs">The <see cref="XObject">XObjects</see>.</param>
    /// <param name="options"></param>
    /// <returns>An enumeration of results, which may contain <see cref="XObject">XObjects</see>, and/or
    /// <see cref="string">strings</see>, <see cref="double">doubles</see> or <see cref="bool">booleans</see>,
    /// the value types are thrown away on subsequent filtering.</returns>
    IEnumerable<object> Filter(IEnumerable<XObject> inputs, DynamicXOptions options);
}