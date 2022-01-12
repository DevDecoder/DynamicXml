using System;
using System.Collections.Generic;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Enumeration controlling what should happen when a method is not found.
/// </summary>
public enum InvokeResultIfNotFound
{
    /// <summary>
    ///     Returns <see langword="null" /> if the method is not found.
    /// </summary>
    Null,

    /// <summary>
    ///     Returns <see cref="IEnumerable{T}">an enumeration</see> of <see langword="dynamic" /> if the method is not found.
    /// </summary>
    Empty,

    /// <summary>
    ///     Throws a <see cref="InvalidOperationException" /> if the method is not found.
    /// </summary>
    Throw,

    /// <summary>
    ///     The default option (<see cref="Empty" />)
    /// </summary>
    Default = Empty
}