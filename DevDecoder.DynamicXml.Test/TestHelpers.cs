using System;
using System.Reflection;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml.Test;

public static class TestHelpers
{
    /// <summary>
    ///     Gets the <paramref name="filename">embedded XML file</paramref> as a <see cref="XDocument" />.
    /// </summary>
    /// <param name="filename">The embedded file's name.</param>
    /// <returns>A <see cref="XDocument" />.</returns>
    public static XDocument GetXml(string? filename = null)
    {
        filename ??= "Test.xml";
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"{typeof(TestHelpers).Namespace}.{filename}")!;
        return XDocument.Load(stream);
    }

    /// <summary>
    ///     Gets the <paramref name="filename">embedded XML file</paramref> as a <see langword="dynamic" />.
    /// </summary>
    /// <param name="filename">The embedded file's name.</param>
    /// <returns>A <see langword="dynamic" />.</returns>
    public static dynamic GetTestDocument(string? filename = null)
    {
        return GetXml(filename).ToDynamic();
    }
}