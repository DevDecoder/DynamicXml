using System.Xml.Linq;
using BenchmarkDotNet.Attributes;
using DevDecoder.DynamicXml.Benchmarks;

public class Search
{
    public static IEnumerable<XDocument> Documents => XmlArticles.AllDocuments();

    [ParamsSource(nameof(Documents))]
    public XDocument Document;

    private XElement _target;

    [IterationSetup]
    public void PickElement()
    {
        _target = Document.RandomElement();
        Console.WriteLine($"Picked Target: {_target.Describe()}");
    }

    [IterationSetup(Target = nameof(FindByName))]
    public void GetNames()
    {
        var names = new List<XName>();
        var current = _target;
        while (current is not null)
        {
            names.Add(current.Name);
            current = current.Parent;
        }

        names.Reverse();
        _names = names.ToArray();
    }

    private IEnumerable<XName> _names;
    [Benchmark]
    public XElement FindByName() => _names.Aggregate(Document.Root, (current1, name) => current1!.Element(name))!;
}