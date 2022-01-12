using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Extension methods.
/// </summary>
public static class Extensions
{
    /// <summary>
    ///     Converts an <see cref="XObject" /> into a dynamic object.
    /// </summary>
    /// <param name="xObject">The <see cref="XObject" />.</param>
    /// <param name="options">The options.</param>
    /// <returns>A dynamic object.</returns>
    [return: NotNullIfNotNull("xObject")]
    public static dynamic? ToDynamic(this XObject? xObject, DynamicXmlOptions? options = null)
    {
        if (xObject is null)
            return null;

        // We cache the DynamicXObject onto the object
        var result = xObject.Annotation<DynamicXObject>();
        if (result is null)
        {
            result = new DynamicXObject(xObject, options);
            xObject.AddAnnotation(result);
        }

        return result;
    }
}