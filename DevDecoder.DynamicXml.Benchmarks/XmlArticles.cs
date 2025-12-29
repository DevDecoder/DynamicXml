using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml.Benchmarks;

public static class XmlArticles
{
    public static readonly XDocument EmptyRoot = new XDocument(new XElement("Root"));
    public static readonly XDocument Simple = XDocument.Parse("<Root><Node attribute=\"value\"/></Root>");
    public static XDocument Sample => LazySample.Value;
    public static XDocument Large => LazyLarge.Value;

    public static IEnumerable<XDocument> AllDocuments()
    {
        yield return EmptyRoot;
        yield return Simple;
        yield return Sample;
        yield return Large;
    }

    private static readonly Lazy<XDocument> LazySample =
        new Lazy<XDocument>(() =>
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"{typeof(XmlArticles).Namespace}.Sample.xml")!;
            return XDocument.Load(stream);
        }, LazyThreadSafetyMode.ExecutionAndPublication);


    private static readonly Lazy<XDocument> LazyLarge =
        new Lazy<XDocument>(() =>
        {
            // Total nodes = (width^(depth - 1)) + (width^(depth - 2)) + (width^(depth - 3)) + ... + (width^0))
            // = 2,097,152 + 262,144 + 32,768 + 4,096 + 512 + 64 + 8 + 1
            // = 2,396,745 nodes
            const int width = 8; // 26 is maximum width (as converted to ASCII 'z').
            const int depth = 8;

            var xDoc = new XDocument();

            // Build large Xml document
            var current = new XElement("X");
            xDoc.Add(current);
            var count = 1;
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                int childCount;
                if (current.Name.LocalName.Length >= depth || (childCount = current.Elements().Count()) >= width)
                {
                    current = current.Parent;
                    if (current is null) break;
                    continue;
                }

                var el = new XElement(XName.Get(current.Name.LocalName + (char) (97 + childCount),
                    current.Name.NamespaceName));
                current.Add(el);
                count++;
                current = el;
            }

            Console.WriteLine($"{count} nodes in {stopwatch.ElapsedMilliseconds}ms");
            return xDoc;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    
    public static XObject RandomObject(this XDocument document, Random? random = null)
    {
        random ??= new Random();
        var node = RandomElement(document.DescendantNodes(), random);
        switch (node)
        {
            case XElement xElement:
                var attributeCount = xElement.Attributes().Count();
                if (attributeCount > 0 && random.Next(2) > 0)
                {
                    return xElement.Attributes().ElementAt(random.Next(attributeCount));
                }

                return xElement;
            default:
                return node;
        }
    }

    public static XNode RandomNode(this XDocument document, Random? random = null) => RandomElement(document.DescendantNodes(), random);

    public static XElement RandomElement(this XDocument document, Random? random = null) => RandomElement(document.Descendants(), random);

    public static T RandomElement<T>(this IEnumerable<T> enumerable, Random? random = null)
    {
        // ReSharper disable PossibleMultipleEnumeration
        var count = enumerable.Count();
        if (count < 1) throw new ArgumentException("No items in enumeration", nameof(enumerable));
        random ??= new Random();
        return enumerable.ElementAt(random.Next(count));
        // ReSharper restore PossibleMultipleEnumeration
    }

    public static string Describe(this XObject xObject) => xObject switch
    {
        XAttribute attribute => $"{attribute.Name}=\"{attribute.Value}\"",
        XComment comment => $"<!--{comment.Value}-->",
        XDocument document => "<DOCUMENT>",
        XElement element => $"<{element.Name}>",
        XProcessingInstruction processingInstruction => $"<?{processingInstruction.Target}>",
        XText text => text.Value,
        XDocumentType documentType => $"<!{documentType.Name}>",
        _ => $"<{xObject.GetType()}>"
    };
}