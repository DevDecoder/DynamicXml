using System.Collections.Generic;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Interface for filters, can be supplied as index.
/// </summary>
public interface IDynamicXFilter
{
    /// <summary>
    ///     Lazily filters the <paramref name="inputs">input</paramref> <see cref="XObject">XObjects</see>.
    /// </summary>
    /// <param name="inputs">The <see cref="XObject">XObjects</see>.</param>
    /// <param name="options"></param>
    /// <returns>
    ///     An enumeration of results, which may contain any object; however, any non-<see cref="XObject" /> will be
    ///     discarded by any subsequent filtering.
    /// </returns>
    IEnumerable<object> Filter(IEnumerable<XObject> inputs, DynamicXOptions options);
}