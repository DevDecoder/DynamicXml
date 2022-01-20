[![Publish](https://github.com/DevDecoder/DynamicXml/workflows/Build%20and%20Publish/badge.svg)](https://github.com/DevDecoder/DynamicXml/actions?query=workflow%3A%22Build+and+Publish%22)
[![Nuget](https://img.shields.io/nuget/v/DevDecoder.DynamicXml)](https://www.nuget.org/packages/DevDecoder.DynamicXml/)

# DynamicXml

**NOTE: This package is currently in development and testing but will be deployed soon.**

Convenience extension method that converts any `XObject` into a `dynamic` for easy access, combined with a powerful XML
filter.

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

To use include the NuGet in your solution and call the `dynamic? ToDynamic(DynamicXOptions?)` extension method on
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

This XML sample can be seen in full [here](DevDecoder.DynamicXml.Test/Test.xml), but is truncated for readability,
further you can see the sample code [here](DevDecoder.DynamicXml.Test/TestSamples.cs):

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

The following sample shows how to conveniently navigate the [above XML](#example) (using the `document`
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
```

# Dynamic members

The dynamic object exposes the following members (note they are not available until runtime, so do not appear in
intelli-sense, etc.):

## Properties

When you request a property, e.g. `address.Name` from the dynamic `address` it looks for `Name` in the following order:

1. The first **Attribute** with
   a [local name](https://docs.microsoft.com/en-us/dotnet/api/system.xml.linq.xname.localname?view=net-6.0) that
   matches, in which case the result is a dynamic `XAttribute`.
2. The first child **Element** with
   a [local name](https://docs.microsoft.com/en-us/dotnet/api/system.xml.linq.xname.localname?view=net-6.0) that
   matches, in which case the result is a dynamic `XElement`.
3. Any field/property on the underlying `XObject` that matches, in which case the result is the same type as that
   property.

If it doesn't find any of the above, then the result depends on the `PropertyResultIfNotFound` option, but, by default
is `null`. This means that searching for a missing attribute/element doesn't throw an exception by default.

## Methods

When you invoke a method, _with no arguments_, from the dynamic it looks for the following (in order, returning the
first time it finds any):

1. All **Attributes** with
   a [local name](https://docs.microsoft.com/en-us/dotnet/api/system.xml.linq.xname.localname?view=net-6.0) that match;
   if any, in which case the result is an enumeration of dynamic `XAttribute`s.
2. All **Elements** with
   a [local name](https://docs.microsoft.com/en-us/dotnet/api/system.xml.linq.xname.localname?view=net-6.0) that match;
   if any, in which case the result is an enumeration of dynamic `XElement`s.

And finally, (even when arguments are supplied):

3. Whether the name maps to 'Filter', in which case it calls the extension
   method `DynamicXObject.Filter(this XObject, DynamicXOptions, params object[])`, and performs a filter operation with
   the supplied filters (if any); the result is the an enumeration, where any `XObject`s are converted to their dynamic
   form ([see below](#filter-method)).
4. Any method on `XObject` that matches the requested signature, in which case the result is the same type as the return
   type of that method.

If it doesn't find any of the above, then the result depends on the `InvokeResultIfNotFound` option, but, by default is
an empty enumeration. This means that searching for a collection of attributes or elements that is missing doesn't throw
an exception by default, but returns an empty enumeration, which is more usable.

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

## Indexer

Easily the most powerful feature is the indexer system, that allows for complex, and efficient, navigation of the
document.

The dynamic accepts a multidimensional indexer, where each index can be either any of the following:

### Integer indices

An integer (`int`, `short`, `ushort`, `sbyte`, `byte`) index `i` will return the `i`th child `XNode` of the current
object as a dynamic. Only `XContainer` objects (i.e. `XElement` or `XDocument`) have children, also we can't use this to
find an attribute as that isn't an `XNode`, (to do that we can use a string, or the `DXAttribute` filter, which we cover
below). For example:

```csharp
// We can use our multi-dimensional index to select nodes, here we select the first node of the document
// (i.e. the Root - 'PurchaseOrders'), then its first child node, which is an XComment.
// Result: ' A comment '
Console.WriteLine(document[0, 0].ToString());
```

If the index is negative, then it will count from the end, e.g. and index of `-1` will get the last child, however,
since C#8 it has been possible to use...

### Index indices

A `System.Index` can be used instead of an integer, indeed, integers are converted to `System.Index` under the hood, so
this is the native accepted by indexers, for example:

```csharp
// We can also use System.Index, e.g. this gets the root, then the last child 3 times, which gives us the last
// Item, it then writes out the PartNumber (898-AM)
Console.WriteLine(document[0, ^1, ^1, ^1].PartNumber.ToString());
```

### String indices

A `string` can be supplied, and

* if it is a valid XML local name (see
  the [`XmlConvert.VerifyNCName` documentation](https://docs.microsoft.com/en-us/dotnet/api/system.xml.xmlconvert.verifyncname?view=net-6.0))
  it will match any attributes with the name and any child nodes with the name (if an `XELement`) or target (if
  an `XProcessingInstruction`), e.g.

```csharp
// We can get access nodes and attributes by string, when we supply a string, it first searches attributes on
// the current node, then children for a matching element (or processing instruction).  Note, this filter actually
// returns mutliple resuls (one for each `PurchaseOrder` with a `PurchaseOrderNumer` attribute), but as we're using an
// indexer, only the first is returned.
// Result: `99503`
Console.WriteLine(document["PurchaseOrders", "PurchaseOrder", "PurchaseOrderNumber"].ToString());
```

* otherwise it will evaluate the string as an [XPath expression](https://developer.mozilla.org/en-US/docs/Web/XPath)
  from the current node, for example, the equivalent XPath would be:

```csharp
// The equivalent XPath
// Result: `99503`
Console.WriteLine(document["/PurchaseOrders/PurchaseOrder/@PurchaseOrderNumber"].ToString());
```

**Note** we could write the XPath as

```xpath
string(/PurchaseOrders/PurchaseOrder[1]/@PurchaseOrderNumber)
```

_The above highlights a common gotcha - annoyingly XPath's are 1-indexed, unlike the C# language which is 0-indexed,
hence [1] returns the first purchase order._

However, this isn't actually _equivalent_ (though it is more specific). Firstly it returns a string and not the
actual `XAttribute`, whereas the two indexers shown both return a dynamic `XAttribute`; secondly, and more subtly, the
original indices actually specify all such attributes, however, the indexers only return the first one. Due to lazy
evaluation this isn't as problematic as you may fear.

This also demonstrates that the potential result of an [XPath](https://developer.mozilla.org/en-US/docs/Web/XPath) is
broader than just a dynamic `XObject`:

* If the XPath leads to a `bool` (e.g. by using
  the [`boolean()` function](https://developer.mozilla.org/en-US/docs/Web/XPath/Functions/boolean)), `double` or
  a `string` then it must be the final index as the result is not an `XContainer` (document or element), so no further
  filtering will be possible.
* If the XPath refers to one or more `XObject`s, then an enumeration of `XObjects`s is returned to the next step.
* If the XPath refers to an `XContainer` (i.e. an `XDocument` or `XElement`), then further index dimensions can be used
  to continue navigation.

### Range indices

As we've begun to explain, the initial result of an indexing step is an enumeration of matches. Usually, this
enumeration will contain zero or one elements, but it doesn't have to. The next indexing step will be applied to all the
previous results, filtering them further. Only when the indexer returns will the first element be selected. As we will
soon see, we can avoid this final step by using the `Filter` functionality directly.

However, that does mean that it is entirely valid to pass a `System.Range`, (e.g. `1..3`, `3..1`, `0..^0`, `..`, etc.),
this may seem useless, but, due to above behaviour, ranges can still be valuable when they are not used as the last
index:

```csharp
// Here we use the `..` range (i.e. all) to explicitly say we're searching for any child node of the root
// element that contains an attribute, element or processing instruction with the specified name/target.
// Result: `99503`
Console.WriteLine(document[0, .., "PurchaseOrderNumber"].ToString());
```

As with `System.Index`, passing a range directly will only filter child nodes (i.e. not attributes), however, we can use
filters to filter attributes with ranges.

### Enumerable indices

The index can also be an enumeration, if the indexer doesn't recognise the supplied index type, then it finally checks
to see if has been given an enumeration, in which case it yields the contents. This is done 'recursively' in a process
called
'flattening', so there is no depth limit (it doesn't use the runtime stack to recurse). One use of this functionality is
to allow an pre-defined array to be passed directly, e.g.:

```csharp
// Our indexers will 'flatten' any enumeration (recursively), which is useful as it allows us to create complex
// reusable indices, which can be passed to multiple calls
// Result: `99503`
var allPurchaseOrders = new object[] {0, "PurchaseOrder"};
Console.WriteLine(document[allPurchaseOrders, "PurchaseOrderNumber"].ToString());
```

### Filter indices

In reality, all indices are converted to one (or more) filters and applied to the result of the previous filter
operation. A filter is any implementation of `IDynamicXFilter`, and is covered in more detail below. Most of the above
indices are converted to two filters:

* Integer or `System.Index` indices are converted to `DXFilter.Children` followed by `DXObject.At()`, e.g.
  `[^1]` ≡ `[DXFilter.Children, DXObject.At(^1)]`.

```csharp
// When we pass an integer or Index as filter, it is first converted to a call to get any children, and then a
// call to get the object at the specified index, so the above call to
// Console.WriteLine(document[0,0].ToString());
// is actually equivalent to-
Console.WriteLine(document[DXFilter.Children, DXObject.At(0), DXFilter.Children, DXObject.At(0)].ToString());
```

* A `string` 'name' index is converted to `DXFilter.AttributesAndChildren` followed by `DXObject.WithName()`, e.g.
  `["PurchaseOrders"]` ≡ `[DXFilter.AttributesAndChildren, DXObject.WithName("PurchaseOrders")]`.
* A `string` 'XPath' index is converted to `DXPath.WithPath`.
* A `System.Range` is converted to `DXFilter.Children` followed by `DXObject.Span()`, e.g.
  `[..]` ≡ `[DXFilter.Children, DXObject.Span(..)]`.
* Any index that is an enumeration is flattened.

We can, of course, specify filters directly to perform extremely powerful (and infinitely extendable) indexing:

```csharp
// In fact, we can specify any filters as indexers, at this point we get an insight into what is really going on
// The indexer actually uses the Filter method under the hood, and returns the first item if any, otherwise
// 'null' (or it can throw an OutOfRangeException if Options.IndexResultIfNotFound is set to Throw).
//
// However, the narrowing down to a single result only occurs as the last step, so here we select all descendant
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
```

### Final Index result

As mentioned above, each indexing step filters the results (plural) of the previous step. This 'filtering' can actually
increase the number of results (e.g. by selecting children). Only in the final step do we select the first result and
return it.

If the indexer doesn't find at least one result, then the result depends on the `IndexResultIfNotFound`
option; by default it returns a `null`, but you can change the behaviour to throw an `IndexOutOfRangeException` instead.

## Filter method

Indexing is very powerful, but the final step can potentially throw away useful results. The convention for the dynamics
is that a property or indexer returns a single result, and a method is used to access multiple results, it should come
as no surprise that we have supplied a builtin method called `Filter` to return all the results of a set of filtering
operations (note, that we can customize dynamic name mapping to avoid collisions
using `DynamicXOptions.MapToBuiltInName` to map a different name to `Filter`).

For example:

```csharp
foreach (XElement element in document.Filter(
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
{
    Console.WriteLine($"{element.Name.LocalName} = {element.Value}");
}
```

However, we can use the filtering system entirely independently of dynamics...

# Filtering

## Introduction

The dynamic `Filter` built-in method and the indexer all use the static `DynamicXObject.Filter` extension methods, by
passing themselves in as the starting `XObject` along with their `DynamicXOptions`, and converting any
resulting `XObject`s to their dynamic form before returning.

```csharp
public static partial class DynamicXObject
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<object> Filter(this XObject input, params object[] indices)
        => Filter(input.ToEnumerable(), null, indices);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<object> Filter(this XObject input, DynamicXOptions? options, params object[] filters)
        => Filter(input.ToEnumerable(), options, filters);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<object> Filter(this IEnumerable<XObject> inputs, params object[] indices)
        => Filter(inputs, null, indices);

    /// <summary>
    /// Filters the <paramref name="inputs">input</paramref> <see cref="XObject">XObjects</see>.
    /// </summary>
    /// <param name="inputs">The <see cref="XObject">XObjects</see>.</param>
    /// <param name="options">The <see cref="DynamicXOptions">options</see>, if any; otherwise <see langword="null"/>.</param>
    /// <param name="filters">The filters.</param>
    /// <returns>An enumeration of results, which may contain <see cref="XObject">XObjects</see>, and/or
    /// <see cref="string">strings</see>, <see cref="double">doubles</see> or <see cref="bool">booleans</see>.</returns>
    public static IEnumerable<object> Filter(this IEnumerable<XObject> inputs, DynamicXOptions? options,
        params object[] filters)
    { .. }
}
```

However, we can use the filter methods directly to avoid the overhead of dynamics when not required, e.g. we could have
written the above loop:

```csharp
// Using the filter method with a dynamic is somewhat inefficient, as we can use it on an XObject directly
// So here we cast our dynamic directly to the XObject before calling filter.  In this case we 
// get our result as an enumeration of objects, which we can cast directly to XElements
foreach (XElement element in ((XDocument) document).Filter(...)) { ... }
```

Of course, if we have the original `XDocument` or `XObject` we can avoid dynamics entirely, and some users may wish to
use the library purely for the filter functionality and the power and convenience it offers.

As already explained, all arguments are converted to one or more filters and applied to the result of a previous filter
operation. A filter is any implementation of `IDynamicXFilter`, allowing for easy extensibility. The interface specifies
a single method, which is reminiscent of the `Filter` extension methods:

```csharp
/// <summary>
/// Lazily filters the <paramref name="inputs">input</paramref> <see cref="XObject">XObjects</see>.
/// </summary>
/// <param name="inputs">The <see cref="XObject">XObjects</see>.</param>
/// <param name="options"></param>
/// <returns>An enumeration of results, which may contain any object; however, any non-<see cref="XObject"/> will be
/// discarded by any subsequent filtering.</returns>
IEnumerable<object> Filter(IEnumerable<XObject> inputs, DynamicXOptions options);
```

As you can see, although it only accepts an enumeration of `XObject` it returns an enumeration of `object`, this is to
allow the filter to return any results it likes, which is important for XPaths, which can return a `string`, `double`
or `bool` value as well as enumeration of `XObject`s. As noted though, any non-`XObject` result is discarded by
subsequent filters, so if you create a filter that doesn't return any `XObject`s you should clearly indicate it is only
useful as a 'final' filter.

A large number of filters are included in the library already, all are found in structs and have the naming
convention `DX[Name]`.

## Navigation Filters

The `DXFilter` struct provides an easy way to create your own filters, by supplying a filter function, but, more
importantly, contains a number of built-in filters focussed primarily on 'navigation' around the XML tree.

## Type filters

Alongside `DXFilter` we have a number of structs that match the different `XObject` types.

* `DXNode`, `DXComment`, `DXText` and `DXCData` will filter the input for the corresponding type.
* `DXElement`, `DXAttribute` will filter the input for the corresponding type, and can optionally filter on the `Name`.
* `DXProcessingInstruction` will filter the input for the corresponding type, and can optionally filter on the `Target`.
* `DXObject` will match any `XObject`, but can optionally match the `DXElement`, `DXAttribute` `Name`, and/or
  the `DXProcessingInstruction` `Target`

All the type filters expose a `Range`, and can be constructed with either a `Range`, an `Index` or an integer (the last
two are effectively ranges with a single entry). Not only can this allow for a subset of results, it can even be used to
reverse the order, by using a range such as `^0..0` (or `DXObject.Reverse`). Note, that due to the limitation of `Range`
and `Index`, when we specify a range where `Range.Start` > `Range.End`, we treat the start as _exclusive_ and the end
as _inclusive_ (i.e. the lower index is always `inclusive`, and the upper index _exclusive_), as such a range of `4..1`,
will return items from index _3_ to _1_ (as _4_ is exclusive being the higher index).

## XPath filters

As [encountered above](#filter-indices), the `DXPath` filter that introduces the full power
of [XPath expressions](https://developer.mozilla.org/en-US/docs/Web/XPath), allowing for XPath evaluation. Like the type
filters, it too can have an optional `Index` or `Range` to further restrict the result set.

# Customizing behaviour

The `dynamic? ToDynamic(DynamicXOptions?)` extension method and the `Filter` extension methods (when called directly)
take an optional `DynamicXOptions` object that allows for powerful customization of how the system works.

**TODO Document options with examples**

# Performance

_This is using dynamics, so if you are using it you are concerned with convenience over performance. You're probably a
JavaScript developer at heart and despise type safety and static analysis :fire:!_

_I personally recommend only using this in tool code or whilst prototyping. If you need raw performance you are not
going to beat accessing the `XObject`s directly, or, better still, in some scenarios you simply cannot beat simple text
parsing (e.g. Regular Expressions). However, you will often find that parsing input XML is not the main bottleneck in
many use cases, so remember the old adage that _"performance is an engineering problem"_ and what to optimize your code
until you identify the bottlenecks._

The main way to improve performance is to avoid creation of dynamic objects. This is best done by navigating to the
point of interest before casting to a dynamic. Of course, you can do this using LINQ to XML, though at that point there
is little reason to continue using the library.

The library itself does an excellent job of avoiding creating dynamic objects until it needs to and once created it
caches them again the associated `XObject`, meaning there will be a maximum of one `DynamicXObjectImpl` per `XObject` in
a document (note, however, that using the dynamic also creates the dynamic XObject<see cref="XObject"/>additional
objects -
[see the notes on implementing IDynamicMetaObjectProvider ](https://docs.microsoft.com/en-us/dotnet/api/system.dynamic.idynamicmetaobjectprovider?view=net-6.0))
.

For this reason, you should make liberal use of the [Filtering functionality](#filtering), either directly (preferably)
or via the [indexer functionality](#indexer) to get to the desired node(s), whenever possible, as it too avoids creating
dynamics.

Ultimately, the library allows for very clean code, and actually does quite a bit to prevent some of the common errors (
e.g. by returning an empty enumeration when no nodes are found, rather than `null`). That trade off makes it ideal for
many use cases, where the performance is still more than sufficient.

**TODO Maybe do some benchmarks and add results here**