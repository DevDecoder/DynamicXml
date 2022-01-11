using System;
using System.Reflection;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml.Test;

public static class TestHelpers
{
    private static readonly Lazy<XDocument> _testXml = new(() =>
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"{typeof(TestDefaultOptions).Namespace}.Test.xml")!;
        return XDocument.Load(stream);
    });

    /// <summary>
    ///     Gets the test <see cref="XDocument" />.
    /// </summary>
    /// <returns>A <see cref="XDocument" />.</returns>
    public static XDocument TestXml => _testXml.Value;

    /// <summary>
    ///     Gets the <see cref="TestXml" /> as a dynamic.
    /// </summary>
    /// <returns>A <see cref="DynamicXDocument" />.</returns>
    public static dynamic GetTestDocument()
    {
        return _testXml.Value.ToDynamic();
    }
}