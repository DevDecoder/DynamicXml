using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Provides extension method for converting any <see cref="XObject" /> into a dynamic object.
/// </summary>
public static partial class DynamicXObject
{
    /// <summary>
    ///     Converts an <see cref="XObject" /> into a dynamic object.
    /// </summary>
    /// <param name="xObject">The <see cref="XObject" />.</param>
    /// <param name="options">The options.</param>
    /// <returns>A dynamic object.</returns>
    [return: NotNullIfNotNull("xObject")]
    public static dynamic? ToDynamic(this XObject? xObject, DynamicXOptions? options = null)
    {
        if (xObject is null)
            return null;

        // We cache the DynamicXObjectImpl onto the object
        var result = xObject.Annotation<DynamicXObjectImpl>();
        if (result is not null) return result;

        result = new DynamicXObjectImpl(xObject, options);
        xObject.AddAnnotation(result);
        return result;
    }

    /// <summary>
    /// Filters the <paramref name="input"/> <see cref="XObject"/>.
    /// </summary>
    /// <param name="input">The <see cref="XObject"/>.</param>
    /// <param name="indices">The indices.</param>
    /// <returns>An enumeration of results, which may contain <see cref="XObject">XObjects</see>, and/or
    /// <see cref="string">strings</see>, <see cref="double">doubles</see> or <see cref="bool">booleans</see>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<object> Filter(this XObject input, params object[] indices)
        => Filter(input.ToEnumerable(), null, indices);

    /// <summary>
    /// Filters the <paramref name="inputs">input</paramref> <see cref="XObject">XObjects</see>.
    /// </summary>
    /// <param name="inputs">The <see cref="XObject">XObjects</see>.</param>
    /// <param name="indices">The indices.</param>
    /// <returns>An enumeration of results, which may contain <see cref="XObject">XObjects</see>, and/or
    /// <see cref="string">strings</see>, <see cref="double">doubles</see> or <see cref="bool">booleans</see>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<object> Filter(this IEnumerable<XObject> inputs, params object[] indices)
        => Filter(inputs, null, indices);

    /// <summary>
    /// Filters the <paramref name="input"/> <see cref="XObject"/>.
    /// </summary>
    /// <param name="input">The <see cref="XObject"/>.</param>
    /// <param name="options">The <see cref="DynamicXOptions">options</see>, if any; otherwise <see langword="null"/>.</param>
    /// <param name="indices">The indices.</param>
    /// <returns>An enumeration of results, which may contain <see cref="XObject">XObjects</see>, and/or
    /// <see cref="string">strings</see>, <see cref="double">doubles</see> or <see cref="bool">booleans</see>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<object> Filter(this XObject input, DynamicXOptions? options, params object[] indices)
        => Filter(input.ToEnumerable(), options, indices);

    /// <summary>
    /// Filters the <paramref name="inputs">input</paramref> <see cref="XObject">XObjects</see>.
    /// </summary>
    /// <param name="inputs">The <see cref="XObject">XObjects</see>.</param>
    /// <param name="options">The <see cref="DynamicXOptions">options</see>, if any; otherwise <see langword="null"/>.</param>
    /// <param name="indices">The indices.</param>
    /// <returns>An enumeration of results, which may contain <see cref="XObject">XObjects</see>, and/or
    /// <see cref="string">strings</see>, <see cref="double">doubles</see> or <see cref="bool">booleans</see>.</returns>
    public static IEnumerable<object> Filter(this IEnumerable<XObject> inputs, DynamicXOptions? options,
        params object[] indices)
    {
        options ??= DynamicXOptions.Default;
        // ReSharper disable PossibleMultipleEnumeration
        IEnumerable<object> lastResult = inputs;

        var stack = new Stack<IEnumerator>();
        stack.Push(indices.GetEnumerator());

        foreach (var filter in indices.Flatten(options.IndexResultIfNotFound == IndexResultIfNotFound.Throw))
        {
            if (!inputs.Any()) break;
            lastResult = filter.Filter(inputs, options);
            inputs = lastResult
                .OfType<XObject>(); // If we go again, we only want to use XObjects as input to next filter.
        }

        return lastResult;
        // ReSharper restore PossibleMultipleEnumeration
    }

    /// <summary>
    /// Converts a single <see cref="XObject"/> into an enumerable.
    /// </summary>
    /// <param name="input">The input object.</param>
    /// <returns>An enumeration that yields the object.</returns>
    private static IEnumerable<XObject> ToEnumerable(this XObject input)
    {
        yield return input;
    }

    /// <summary>
    /// Flattens an input array of indices into an <see cref="IEnumerable{T}">enumeration</see> of <see cref="IDynamicXFilter"/>.
    /// </summary>
    /// <param name="indices">The indices.</param>
    /// <param name="throwIfInvalidIndex"><see langword="true"/> if should throw <see cref="IndexOutOfRangeException"/> for invalid index type.</param>
    /// <returns>An <see cref="IEnumerable{T}">enumeration</see> of <see cref="IDynamicXFilter"/></returns>
    private static IEnumerable<IDynamicXFilter> Flatten(this object[] indices, bool throwIfInvalidIndex)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(indices.GetEnumerator());

        while (stack.Any())
        {
            var enumerator = stack.Pop();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                switch (current)
                {
                    case IDynamicXFilter filter:
                        yield return filter;
                        continue;

                    /*
                     * Special case strings
                     */
                    case string str:
                        string? name;
                        try
                        {
                            name = XmlConvert.VerifyNCName(str);
                        }
                        catch (XmlException)
                        {
                            name = null;
                        }

                        yield return DXFilter.Children;
                        yield return name is null ? DXPath.WithPath(str) : DXObject.WithName(name);
                        continue;

                    /*
                     * Special case integer literals, to index child objects
                     */
                    case int v:
                        yield return DXFilter.Children;
                        yield return DXObject.At(v);
                        continue;
                    case short v:
                        yield return DXFilter.Children;
                        yield return DXObject.At(v);
                        continue;
                    case sbyte v:
                        yield return DXFilter.Children;
                        yield return DXObject.At(v);
                        continue;
                    case ushort v:
                        yield return DXFilter.Children;
                        yield return DXObject.At(v);
                        continue;
                    case byte v:
                        yield return DXFilter.Children;
                        yield return DXObject.At(v);
                        continue;

                    /*
                     * If we're passed an enumerable, flatten.
                     */
                    case IEnumerable enumerable:
                        stack.Push(enumerator);
                        enumerator = enumerable.GetEnumerator();
                        continue;

                    // Anything else is an unsupported index type.
                    default:
                        if (throwIfInvalidIndex)
                        {
                            throw new IndexOutOfRangeException(
                                $"{current?.GetType().ToString() ?? "null"} is not a valid index");
                        }

                        // Just skip
                        continue;
                }
            }
        }
    }


    /// <summary>
    ///     Returns the number as an ordinal.
    /// </summary>
    /// <param name="number">The number</param>
    /// <param name="provider">The format provider</param>
    /// <returns>The ordinal string.</returns>    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string ToOrdinal(this long number, IFormatProvider? provider = null)
    {
        var abs = Math.Abs(number);
        return abs / 10 == 1
            ? string.Create(provider, stackalloc char[64], $"{number}th")
            : (abs % 10) switch
            {
                1 => string.Create(provider, stackalloc char[64], $"{number}st"),
                2 => string.Create(provider, stackalloc char[64], $"{number}nd"),
                3 => string.Create(provider, stackalloc char[64], $"{number}rd"),
                _ => string.Create(provider, stackalloc char[64], $"{number}th")
            };
    }
}