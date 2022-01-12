using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace DevDecoder.DynamicXml.Test;

public class TestDefaultOptions
{
    [Fact]
    public void TestForEach()
    {
        var document = TestHelpers.GetTestDocument();
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
    public void TestBuiltIns()
    {
        var document = TestHelpers.GetTestDocument();
        XDocument xDocument = document;
        Assert.Equal(xDocument.Declaration, document.Declaration);

        var purchaseOrders = document.PurchaseOrders;
        XElement xPurchaseOrders = purchaseOrders;
        Assert.Equal(xPurchaseOrders.AncestorsAndSelf(), purchaseOrders.AncestorsAndSelf());
    }

    [Fact]
    public void TestCast()
    {
        var document = TestHelpers.GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var second = purchaseOrders[2];
        Assert.NotNull(second);

        DateTime date = second.OrderDate;
        Assert.Equal(new DateTime(1999, 10, 22), date);
    }
}