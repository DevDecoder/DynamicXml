using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DevDecoder.DynamicXml.Test;

public class TestSamples
{
    // HACK: We call our output helper `Console` to allow the sample code to look like console code!
    // ReSharper disable once InconsistentNaming
    private readonly ITestOutputHelper Console;

    public TestSamples(ITestOutputHelper testOutputHelper)
    {
        Console = testOutputHelper;
    }

    [Fact]
    public void SampleIntro()
    {
        // Load XML from embedded file.
        XDocument xDocument;
        using (var stream = Assembly.GetExecutingAssembly()
                   .GetManifestResourceStream($"{GetType().Namespace}.Test.xml")!)
        {
            xDocument = XDocument.Load(stream);
        }

        // Convert to dynamic object
        var document = xDocument.ToDynamic();

        // Get root
        var purchaseOrders = document.PurchaseOrders;

        // Enumerate over all child elements called `PurchaseOrder`
        foreach (var order in purchaseOrders.PurchaseOrder())
        {
            // We can cast directly to type (note this throws exception if we have bad data though
            // so it is usually safer to retrieve the string value and use a TryParse method or equivalent.
            int po = order.PurchaseOrderNumber;
            DateTime date = order.OrderDate;
            Console.WriteLine($"Order# {po} on {date}:");

            // Get the first DeliveryNote child node, if any.
            var deliveryNote = order.DeliveryNotes;

            // By default, if the node is missing we get a null back.
            if (deliveryNote is not null)
                // The default ToString() method of `DynamicXObject` casts to string, so for elements, attributes,
                // comments and text, it returns the inner value.
                Console.WriteLine($"  Note: {deliveryNote}");

            // We can also enumerate over Address nodes
            foreach (var address in order.Address())
            {
                Console.WriteLine($"  {address.Type} Address:");
                Console.WriteLine($"    {address.Name}");
                Console.WriteLine($"    {address.Street}");
                Console.WriteLine($"    {address.City}");
                Console.WriteLine($"    {address.State}");
                Console.WriteLine($"    {address.Zip}");
                Console.WriteLine($"    {address.Country}");
            }

            // If there is no Items node, `order.Items` will return `null` and the check will fail.
            // If there are any `Item` nodes, then the `?.Item()` will return an `IEnumerable<dynamic>`
            // the `is` check allows us to confirm this safely in one step, and also allows us to use LINQ operations
            // on the result (we can't use LINQ directly on the returned `dynamic` so the cast exposes the enumeration
            // first).
            if (order.Items?.Item() is IEnumerable<dynamic> items)
            {
                // Cast to array to prevent multiple enumeration, for performance, our dynamics dynamically enumerate.
                var itemsArr = items.ToArray();

                // Write out order summary
                Console.WriteLine(
                    $"  {itemsArr.Length} item{(itemsArr.Length == 1 ? "" : "s")}" +
                    // Note the `double` cast below will error if `USPrice` isn't a valid double.
                    // A safer method would be to use a TryParse instead.
                    $" Total: ${itemsArr.Aggregate(0D, (total, item) => total + (double) item.USPrice):F2}");

                // Iterate over each `Item` node
                foreach (var item in items) Console.WriteLine($"    {item.PartNumber} @ ${item.USPrice}");
            }

            Console.WriteLine("");
        }
    }

    [Fact]
    public void SampleIndexers()
    {
        // Get XML document
        var xDoc = TestHelpers.GetXml();

        // Get dynamic document
        var document = xDoc.ToDynamic();

        // We can use our multi-dimensional index to select nodes, here we select the first node of the document
        // (i.e. the Root - 'PurchaseOrders'), then its first child node, which is an XComment.
        // Result: ' A comment '
        Console.WriteLine(document[0, 0].ToString());

        // We can also use System.Index, e.g. this gets the root, then the last child 3 times, which gives us the last
        // Item, it then writes out the PartNumber (898-AM)
        Console.WriteLine(document[0, ^1, ^1, ^1].PartNumber.ToString());

        // We can get access nodes and attributes by string, when we supply a string, it first searches attributes on
        // the current node, then children for a matching element (or processing instruction).
        // Result: `99503`
        Console.WriteLine(document["PurchaseOrders", "PurchaseOrder", "PurchaseOrderNumber"].ToString());

        // The equivalent XPath
        // Result: `99503`
        Console.WriteLine(document["string(/PurchaseOrders/PurchaseOrder[1]/@PurchaseOrderNumber)"].ToString());

        // Here we use the `..` range (i.e. all) to explicitly say we're searching for any child node of the root
        // element that contains an attribute, element or processing instruction with the specified name/target.
        // Result: `99503`
        Console.WriteLine(document[0, .., "PurchaseOrderNumber"].ToString());

        // Our indexers will 'flatten' any enumeration (recursively), which is useful as it allows us to create complex
        // reusable indices, which can be passed to multiple calls
        // Result: `99503`
        var allPurchaseOrders = new object[] {0, "PurchaseOrder"};
        Console.WriteLine(document[allPurchaseOrders, "PurchaseOrderNumber"].ToString());

        // When we pass an integer or Index as filter, it is first converted to a call to get any children, and then a
        // call to get the object at the specified index, so the above call to
        // Console.WriteLine(document[0,0].ToString());
        // is actually equivalent to-
        Console.WriteLine(document[DXFilter.Children, DXObject.At(0), DXFilter.Children, DXObject.At(0)].ToString());

        // In fact, we can specify any filters as indexers, at this point we get an insight into what is really going on
        // The indexer actually uses the Filter method under the hood, and returns the first item if any, otherwise
        // 'null' (or it can throw an OutOfRangeException if Options.IndexResultIfNotFound is set to Throw).
        //
        // However, the narrowing down to a single result only occurs as the last step, so  here we select all descendant
        // nodes of the document, and then find the last comment. ('A comment')
        Console.WriteLine(document[DXFilter.Descendants, DXComment.At(^1)].ToString());

        // Here's another example which finds elements called `Item`, with an `Attribute` called `PartNumber`, and selects
        // the second one it sees ('926-AA')
        Console.WriteLine(string.Join(
            Environment.NewLine,
            document[
                DXFilter.DescendantsAndSelf,
                DXElement.WithName("Item"),
                DXFilter.Attributes,
                DXAttribute.WithName("PartNumber", 1)
            ]));
    }

    [Fact]
    public void SampleFilter()
    {
        // Get XML document
        var xDoc = TestHelpers.GetXml();
        // Get dynamic document
        var document = xDoc.ToDynamic();

        // We can use the "Filter" with our dynamic XObjects
        // NOTE, unlike indexer, we always return an enumeration, that can contain one or more results,
        // or be empty.
        Assert.Equal(
            xDoc.Root!.Nodes().Skip(1).Take(1),
            // The `1` is equivalent to DXFilter.Children, DXObject.At(1)
            document.Filter(DXFilter.Root, 1));

        // Using the filter method with a dynamic is somewhat inefficient, as we can use it on an XObject directly
        // So here we cast our dynamic directly to the XObject before calling filter.  In this case we 
        // get our result as an enumeration of objects, which we can cast directly to XElements
        // foreach (XElement element in document.Filter(...)) {...}
        foreach (XElement element in ((XDocument) document).Filter(
                     // Select the root element.
                     DXFilter.Root,
                     // Then get the direct children
                     DXFilter.Children,
                     // Select the first child with the name 'PurchaseOrder'
                     DXElement.WithName("PurchaseOrder", 0),
                     DXFilter.Children,
                     // Select the last address node
                     DXElement.WithName("Address", ^1),
                     // Finally get all child nodes
                     DXFilter.Children
                 ))
            Console.WriteLine($"{element.Name.LocalName} = {element.Value}");
    }
}