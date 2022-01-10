using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;
using Xunit;

public class UnitTests
{
    private static Lazy<XDocument> _testXml = new Lazy<XDocument>(() =>
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Test.xml")!;
        return XDocument.Load(stream);
    });

    public static dynamic GetTestDocument() => new DynamicXDocument(_testXml.Value);

    [Fact]
    public void TestForEach()
    {
        dynamic document = GetTestDocument();
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
        dynamic document = GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var second = purchaseOrders[1];
        Assert.NotNull(second);
        
        Assert.Equal(((XElement)purchaseOrders).Elements().Skip(1).First(), second);
    }
    
    [Fact]
    public void TestBuiltIns()
    {
        dynamic document = GetTestDocument();
        XDocument xDocument = document;
        Assert.Equal(xDocument.Declaration, document.__Declaration);
        
        var purchaseOrders = document.PurchaseOrders;
        XElement xPurchaseOrders = purchaseOrders;
        Assert.Equal(xPurchaseOrders.AncestorsAndSelf(), purchaseOrders.__AncestorsAndSelf());
    }
    
    [Fact]
    public void TestCast()
    {
        dynamic document = GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var second = purchaseOrders[1];
        Assert.NotNull(second);

        DateTime date = second.OrderDate;
        Assert.Equal(new DateTime(1999,10,22), date);
    }
}