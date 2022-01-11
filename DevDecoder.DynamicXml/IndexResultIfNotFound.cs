using System;

namespace DevDecoder.DynamicXml;

/// <summary>
/// Enumeration controlling what should happen when an index is not found.
/// </summary>
public enum IndexResultIfNotFound
{
    /// <summary>
    /// Returns <see langword="null" /> if the index is not found.  
    /// </summary>
    Null,

    /// <summary>
    /// Throws a <see cref="IndexOutOfRangeException"/> if the index is not found.
    /// </summary>
    Throw,

    /// <summary>
    /// The default option (<see cref="Null"/>)
    /// </summary>
    Default = Null
}