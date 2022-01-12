using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace DevDecoder.DynamicXml.Test;

public class TestIndexer
{
    [Fact]
    public void IntegerIndexShouldSucceed()
    {
        var document = TestHelpers.GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var element = purchaseOrders[1];
        Assert.NotNull(element);

        Assert.Equal(((XElement) purchaseOrders).Nodes().Skip(1).First(), (XElement) element);
    }

    [Fact]
    public void XPathIndexShouldSucceed()
    {
        var document = TestHelpers.GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        // We can grab XComments using XPath
        var element = purchaseOrders["/PurchaseOrders/comment()"];
        Assert.NotNull(element);

        Assert.Equal(((XElement) purchaseOrders).Nodes().OfType<XComment>().First(), (XComment) element);
        Assert.Equal("A comment", ((string) element).Trim());
    }

    [Fact]
    public void MultiDimensionIntegerIndexShouldSucceed()
    {
        var document = TestHelpers.GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var element = purchaseOrders[2, 1];
        Assert.NotNull(element);

        Assert.Equal((((XElement) purchaseOrders).Nodes().Skip(2).First() as XContainer)
            ?.Nodes().Skip(1).First(),
            (XElement) element);
    }

    [Fact]
    public void MultiDimensionMixedIndexShouldSucceed()
    {
        var document = TestHelpers.GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        // Use integer indices to navigate, then the "text()" XPath to extract the second text node
        var element = purchaseOrders[1, 3, 1, "text()[2]"];
        Assert.NotNull(element);

        Assert.Equal(
            (((((XElement) purchaseOrders).Nodes().Skip(1).First() as XContainer)
                    ?.Nodes().Skip(3).First() as XContainer)
                ?.Nodes().Skip(1).First() as XContainer)
            ?.Nodes().OfType<XText>().Skip(1).First(),
            (XText) element);
        Assert.Equal("A second text node!", ((string) element).Trim());
    }

    [Fact]
    public void IntegerIndexAfterXPathShouldSucceed()
    {
        var document = TestHelpers.GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        var element = purchaseOrders["/PurchaseOrders/PurchaseOrder[2]", 2];
        Assert.NotNull(element);

        Assert.Equal(
            (((XElement) purchaseOrders).Nodes().Skip(2).First() as XContainer)
            ?.Nodes().Skip(2).First(),
            (XElement) element);
    }

    [Fact]
    public void FinalNonNodeXPathShouldSucceed()
    {
        var document = TestHelpers.GetTestDocument();
        var purchaseOrders = document.PurchaseOrders;
        Assert.NotNull(purchaseOrders);

        // If the XPAth is at the end we can return non-node values, e.g. a double
        var element = purchaseOrders["number(/PurchaseOrders/PurchaseOrder[1]/Items/Item[1]/USPrice)"];
        Assert.NotNull(element);
        Assert.IsType<double>(element);
        Assert.Equal(148.95D, element);
    }
}