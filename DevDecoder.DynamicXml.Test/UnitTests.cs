using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace DevDecoder.DynamicXml.Test;

public class UnitTests
{
    private static readonly Lazy<XDocument> _testXml = new(() =>
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"{typeof(UnitTests).Namespace}.Test.xml")!;
        return XDocument.Load(stream);
    });

    public static dynamic GetTestDocument()
    {
        return new DynamicXDocument(_testXml.Value);
    }

    [Fact]
    public void TestForEach()
    {
        var document = GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var count = 0;
        foreach (var order in purchaseOrders.PurchaseOrder())
        {
            Assert.NotNull(order);
            count++;
        }

        Assert.Equal(3, count);
    }

    [Fact]
    public void TestIndex()
    {
        var document = GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var second = purchaseOrders[1];
        Assert.NotNull(second);

        Assert.Equal(((XElement) purchaseOrders).Elements().Skip(1).First(), second);
    }

    [Fact]
    public void TestBuiltIns()
    {
        var document = GetTestDocument();
        XDocument xDocument = document;
        Assert.Equal(xDocument.Declaration, document.__Declaration);

        var purchaseOrders = document.PurchaseOrders;
        XElement xPurchaseOrders = purchaseOrders;
        Assert.Equal(xPurchaseOrders.AncestorsAndSelf(), purchaseOrders.__AncestorsAndSelf());
    }

    [Fact]
    public void TestCast()
    {
        var document = GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var second = purchaseOrders[1];
        Assert.NotNull(second);

        DateTime date = second.OrderDate;
        Assert.Equal(new DateTime(1999, 10, 22), date);
    }
}