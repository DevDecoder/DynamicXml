[![Publish](https://github.com/DevDecoder/DynamicXml/workflows/Build%20and%20Publish/badge.svg)](https://github.com/DevDecoder/DynamicXml/actions?query=workflow%3A%22Build+and+Publish%22)
[![Nuget](https://img.shields.io/nuget/v/DevDecoder.DynamicXml)](https://www.nuget.org/packages/DevDecoder.DynamicXml/)

# DynamicXml

Convenience extension method that converts any `XObject` into a `dynamic` for easy access.

# Installation

**TODO Build/Publish NuGet**

~~The library is [available via NuGet](https://www.nuget.org/packages?q=DevDecoder.DynamicXml) and is delivered via
NuGet Package Manager:~~

```
Install-Package DevDecoder.DynamicXml
```

~~If you are targeting .NET Core, use the following command:~~

```
dotnet add package 
Install-Package DevDecoder.DynamicXml
```

# Usage

## Casting to dynamic

To use include the NuGet in your solution and call the `dynamic? ToDynamic(DynamicXmlOptions?)` extension method on
any `XObject`, e.g.

```csharp
// Load XML from embedded file.
XDocument xDocument;
using (var stream = Assembly.GetExecutingAssembly()
           .GetManifestResourceStream($"{this.GetType().Namespace}.Test.xml")!)
    xDocument = XDocument.Load(stream);

// Convert to dynamic object
var document = xDocument.ToDynamic();
```

## Example

This XML sample is taken from
[Microsoft](https://docs.microsoft.com/en-us/dotnet/standard/linq/sample-xml-file-multiple-purchase-orders), but is
truncated for readability:

```xml
<?xml version="1.0" encoding="utf-8"?>
<PurchaseOrders>
    <PurchaseOrder PurchaseOrderNumber="99503" OrderDate="1999-10-20">
        <Address Type="Shipping">
            <Name>Ellen Adams</Name>
            <Street>123 Maple Street</Street>
            <City>Mill Valley</City>
            <State>CA</State>
            <Zip>10999</Zip>
            <Country>USA</Country>
        </Address>
        ...
        <DeliveryNotes>Please leave packages in shed by driveway.</DeliveryNotes>
        <Items>
            <Item PartNumber="872-AA">
                <ProductName>Lawnmower</ProductName>
                <Quantity>1</Quantity>
                <USPrice>148.95</USPrice>
                <Comment>Confirm this is electric</Comment>
            </Item>
            ...
        </Items>
    </PurchaseOrder>
    ...
</PurchaseOrders>
```

The following example shows how to quickly navigate the [above XML](#example) (using the `document`
created [above](#casting-to-dynamic)):

```csharp
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
    {
        // The default ToString() method of `DynamicXObject` casts to string, so for elements, attributes,
        // comments and text, it returns the inner value.
        Console.WriteLine($"  Note: {deliveryNote}");
    }

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
            $" Total: ${itemsArr.Aggregate(0D, (total, item) => total + (double)item.USPrice):F2}");
        
        // Iterate over each `Item` node
        foreach (var item in items)
        {
            Console.WriteLine($"    {item.PartNumber} @ ${item.USPrice}");
        }
    }
    Console.WriteLine("");
}
```

# Dynamic members

The `dynamic` exposes the following members (note they are not available until runtime):

## Properties

When you request a property from the dynamic it looks for the following (in order):

1. The first **Attribute** with
   a [local name](https://docs.microsoft.com/en-us/dotnet/api/system.xml.linq.xname.localname?view=net-6.0) that
   matches.
2. The first child **Element** with
   a [local name](https://docs.microsoft.com/en-us/dotnet/api/system.xml.linq.xname.localname?view=net-6.0) that
   matches.
3. Any field/property on the `XObject` that matches.

If it doesn't find any of the above, then the result depends on the `PropertyResultIfNotFound` option.

## Methods

When you invoke a method, _with no arguments_, from the dynamic it looks for the following (in order, returning the
first time it finds any):

1. All **Attributes** with
   a [local name](https://docs.microsoft.com/en-us/dotnet/api/system.xml.linq.xname.localname?view=net-6.0) that match;
   if any.
2. All **Elements** with
   a [local name](https://docs.microsoft.com/en-us/dotnet/api/system.xml.linq.xname.localname?view=net-6.0) that match;
   if any.

And finally, (even when arguments are supplied):

3. Any method on `XObject` that matches the requested signature.

If it doesn't find any of the above, then the result depends on the `InvokeResultIfNotFound` option.

## Indexer

Easily the most powerful feature is the indexer system, that allows for complex, and efficient, navigation of the
document.

The dynamic accepts a multidimensional indexer, where each index can be either an integer (any integer type
except `UInt64` is allowed), or an [XPath string](https://www.w3schools.com/xml/xpath_syntax.asp).

The Integer index will return for the n'th child _element_ of the current `XNode` (i.e. `XElement` or `XDocument`), as
you specify extra dimensions, it will continue down the tree. For example `purchaseOrders[0]` returns the
first `PurchaseOrder` element, whilst `purchaseOrders[1,1]` returns the second `Address` element of the
second `PurchaseOrder` element.

You can also supply an [XPath](https://developer.mozilla.org/en-US/docs/Web/XPath) which allows for extremely powerful
searching.

* If the XPath leads to a `bool` (e.g. by using
  the [`boolean()` function](https://developer.mozilla.org/en-US/docs/Web/XPath/Functions/boolean)), `double` or
  a `string` then it must be the final index as the result is the type and not another dynamic.
* If the XPath refers to one or more `XObject`s, then the first one is returned as dynamic. For more detail, refer to
  the [documentation for `XPathEvaulate`](https://docs.microsoft.com/en-us/dotnet/api/system.xml.xpath.extensions.xpathevaluate?view=net-6.0)
  .
* If the XPath refers to an `XContainer` (i.e. an `XDocument` or `XElement`), then further index dimensions can be used
  to continue navigation.
* Using XPath indices allows us to access other `XObject` types, like comments (`XComment`) and text (`XText`) which are
  also returned as dynamic objects.

As explained, you can easily mix both indexer types in a single lookup:

```csharp
// The select the first `PurchaseOrder` element, then the 3 child (`Items`), and then the second `Item` element
// Finally it executes the `text()[2]` XPath to return the second text node, which we cast to an `XText` (from the
// dynanic result).
XText text = purchaseOrders[0,3,1,"text()[2]"];
```

Note a subtlety with the way XPaths are interpreted, the above is *NOT* equivalent to:

```csharp
// The following results in `null` (or throws an IndexOutOfRangeException if the `IndexResultIfNotFound` is set to `Throw`
var text = purchaseOrders[0,3,1,"text()",2];
```

The reason as that the `text()` XPath returns an enumeration of `XText` nodes, _of which the first is selected_. Indeed,
if you did not specify the final index the result would be the first `XText` node, so the following are equivalent:

```csharp
XText text = purchaseOrders[0,3,1,"text()[1]"];

// The following is equivalent as we pick the first XObject returned from the XPath.
XText equivalent = purchaseOrders[0,3,1,"text()"];
```

As `XText` nodes do not descend from `XContainer` they do not have children, therefore the `2` index is effectively out
of range. For this reason, if you wish to work with an element other than the first returned by an XPath, you must
specify the index in the XPath itself, rather than in a subsequent index - e.g. the following is valid:

```csharp
// This gets the 3rd child node (1-indexed) from the second purchase order (0-indexed). 
var node = purchaseOrders["/PurchaseOrders/PurchaseOrder[2]", 2];
```

_This highlights another common gotcha, annoyingly XPath's are 1-indexed, unlike the C# language which is 0-indexed._

If the indexer doesn't find a child element at the specified position, then the result depends on
the `IndexResultIfNotFound`
option; by default it returns a `null`, but you can change the behaviour to throw an `IndexOutOfRangeException` instead.

## ToString

When calling `ToString()` on the dynamic is will return the `Value` of the associated `XObject`, if avaiable, or will
call `ToString()` on the associated `XObject` directly (e.g. for an `XDocument` that does not have a `Value`, it will
dump out a string representation of the whole document).

## Casting

Whenever an `XObject` is returned, it too is returned as a `dynamic?` (specifically a `DynamicXmlObject`, though this is
internal). However, you can always cast it to the equivalent `XObject` directly.

For example, if we added the line `xdocument = document;` to the [cast to dynamic example](#casting-to-dynamic), we
extract the
`XDocument` back out of the dynamic:

```csharp
XDocument xDocument;
using (var stream = Assembly.GetExecutingAssembly()
           .GetManifestResourceStream($"{this.GetType().Namespace}.Test.xml")!)
    xDocument = XDocument.Load(stream);

// Convert to dynamic object
var document = xDocument.ToDynamic();

// We can easily cast it back (note `xdocument` is type `XDocument` so a cast occurs here)
xdocument = document;
```

Similarly, we can also cast to a `string`, which is the same as calling `ToString()`, [see above](#tostring).

Unless the `ConvertMode` option is set to `ConvertMode.AsString`, you can also attempt any valid conversion from the
value (if available), and, if that fails, from the underlying `XObject`.

# Customizing behaviour

The `dynamic? ToDynamic(DynamicXmlOptions?)` extension method takes an optional `DynamicXmlOptions` object that allows
for powerful customization of how the system works.

**TODO Document options with examples**

# Performance

This is using dynamics, so if you are using it you are concerned with convenience over performance. You're probably a
JavaScript developer at heart and despise type safety and static analysis.

I personally recommend only using this in tool code or whilst prototyping. If you need raw performance you are not going
to beat accessing the `XObject`s directly, or, better still, in some scenarios you simply cannot beat simple text
parsing (e.g. Regular Expressions). However, you will often find that parsing input XML is not the main bottleneck in
many use cases, so remember the old adage that _"performance is an engineering problem"_ and what to optimize your code
until you identify the bottlenecks.

The main way to improve performance is to avoid creation of dynamic objects. This is best done by navigating to the
point of interest before casting to a dynamic. Of course, you can do this using LINQ to XML, though at that point there
is little reason to continue using the library, or, you should make use of the
[indexer functionality](#indexer) to get to the desired node.

Ultimately, it allows for very clean code, and actually does quite a bit to prevent some of the common errors (e.g. by
returning and empty enumeration when no nodes are found, rather than `null`).

**TODO Maybe do some benchmarks and add results here**