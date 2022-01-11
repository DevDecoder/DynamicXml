using System;

namespace DevDecoder.DynamicXml;

/// <summary>
/// Enumeration controlling what should happen when a property request is not found.
/// </summary>
public enum PropertyResultIfNotFound
{
    /// <summary>
    /// Returns <see langword="null" /> if the property is not found.  
    /// </summary>
    Null,

    /// <summary>
    /// Throws a <see cref="InvalidOperationException"/> if the property is not found.
    /// </summary>
    Throw,

    /// <summary>
    /// The default option (<see cref="Null"/>)
    /// </summary>
    Default = Null
}