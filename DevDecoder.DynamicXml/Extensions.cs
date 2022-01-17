using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DevDecoder.DynamicXml;

public static class Extensions
{
    /// <summary>
    ///     Given an <paramref name="index" />, returns the <see cref="Index" /> index.
    /// </summary>
    /// <param name="index">The index</param>
    /// <returns>The next <see cref="Index" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="index" /> is ^0 or
    ///     <see cref="int.MaxValue" />.
    /// </exception>
    public static Index Next(this Index index)
    {
        return index.IsFromEnd
            ? Index.FromEnd(index.Value - 1)
            : Index.FromStart(index.Value + 1);
    }

    /// <summary>
    ///     Given an <paramref name="index" />, returns the equivalent <see cref="Range" />.
    /// </summary>
    /// <param name="index">The index</param>
    /// <returns>The equivalent <see cref="Range" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="index" /> is ^0 or
    ///     <see cref="int.MaxValue" />.
    /// </exception>
    /// <remarks>
    ///     Returns a <see cref="Range" /> where <see cref="Range.Start" /> equals <paramref name="index" />, and
    ///     <see cref="Range.End" />
    ///     is set to <see cref="Next(Index)">the next position</see>, as such, it represent a single item at the
    ///     <paramref name="index">specified index</paramref>
    /// </remarks>
    public static Range ToRange(this Index index)
    {
        return new(index, index.Next());
    }

    /// <summary>
    ///     Filters an enumeration by only returning the items in the <paramref name="range">specified range</paramref>
    /// </summary>
    /// <param name="source">The source enumeration</param>
    /// <param name="range">The range</param>
    /// <typeparam name="T">The item type</typeparam>
    /// <returns>A enumeration the only includes the <paramref name="range" />.</returns>
    /// <remarks>
    ///     <para>
    ///         Attempts to efficiently apply a <see cref="Range" /> to any enumeration, not just
    ///     </para>
    /// </remarks>
    public static IEnumerable<T> Span<T>(this IEnumerable<T> source, Range range)
    {
        var startIndex = range.Start;
        var endIndex = range.End;
        var start = startIndex.Value;
        var end = endIndex.Value;

        if (start == end &&
            startIndex.IsFromEnd == endIndex.IsFromEnd)
            // Empty range
            return Array.Empty<T>();

        // ReSharper disable PossibleMultipleEnumeration
        int? count;
        if (startIndex.IsFromEnd || endIndex.IsFromEnd)
        {
            // We need a count
            count = source.Count();
            if (startIndex.IsFromEnd)
                start = count.Value - start;
            if (endIndex.IsFromEnd) end = count.Value - end;
        }
        else
        {
            count = null;
        }

        if (start == end)
            // Empty range
            return Array.Empty<T>();

        var reverse = start > end;
        if (reverse) (start, end) = (end, start);

        var result = source;
        // ReSharper restore PossibleMultipleEnumeration

        if (start > 0) result = result.Skip(start);
        if (end != count) result = result.Take(end - start);
        if (reverse) result = result.Reverse();

        return result;
    }

    /// <summary>
    /// Converts a single, nullable <paramref name="input"/> to an enumeration.
    /// </summary>
    /// <param name="input">The input object, if any; otherwise <see langword="null"/>.</param>
    /// <returns>An enumeration of zero or one objects.</returns>
    internal static IEnumerable<T> ToEnum<T>(this T? input)
    {
        if (input is not null) yield return input;
    }
}