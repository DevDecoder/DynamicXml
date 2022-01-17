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
        // Get document
        var document = TestHelpers.GetTestDocument();

        // Get our purchase orders as an enumeration of dynamics
        // Note, dynamic XObjects accept a multi-dimensional index, each dimension navigates further down the tree
        // using the result of the previous dimension.
        foreach (var element in document[
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
                 ])
        {
            Console.WriteLine($"{element.Name.LocalName} = {element}");
        }

        // Get all comments
        Console.WriteLine(string.Join(
            Environment.NewLine,
            document[
                DXFilter.DescendantsAndSelf,
                DXComment.All()
            ]));

        // Get all part numbers
        Console.WriteLine(string.Join(
            Environment.NewLine,
            document[
                DXFilter.DescendantsAndSelf,
                DXElement.WithName("Item"),
                DXFilter.Attributes,
                DXAttribute.WithName("PartNumber")
            ]));
    }
}