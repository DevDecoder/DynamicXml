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

When you try to access an element using an indexer (e.g. `node[0]`), _with a single dimension_, from the dynamic it
looks for the child element at the specified index. It does not search attributes, or expose comments/text.

If it doesn't find a child element at the specified position, or the wrong dimensionality is specified (
e.g. `node[1,2]`), then the result depends on the `IndexResultIfNotFound` option.

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