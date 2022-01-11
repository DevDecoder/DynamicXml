using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

public static class Extensions
{
    [return: NotNullIfNotNull("xObject")]
    public static dynamic? ToDynamic(this XObject? xObject, DynamicXOptions? options = null)
    {
        return xObject is null ? null : new DynamicXObject(xObject, options);
    }
}