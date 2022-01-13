using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Provides extension method for converting any <see cref="XObject" /> into a dynamic object.
/// </summary>
public static partial class DynamicXObject
{
    /// <summary>
    ///     Converts an <see cref="XObject" /> into a dynamic object.
    /// </summary>
    /// <param name="xObject">The <see cref="XObject" />.</param>
    /// <param name="options">The options.</param>
    /// <returns>A dynamic object.</returns>
    [return: NotNullIfNotNull("xObject")]
    public static dynamic? ToDynamic(this XObject? xObject, DynamicXOptions? options = null)
    {
        if (xObject is null)
            return null;

        // We cache the DynamicXObjectImpl onto the object
        var result = xObject.Annotation<DynamicXObjectImpl>();
        if (result is not null) return result;

        result = new DynamicXObjectImpl(xObject, options);
        xObject.AddAnnotation(result);
        return result;
    }
}