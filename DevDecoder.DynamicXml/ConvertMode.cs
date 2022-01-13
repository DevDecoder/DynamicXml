using System;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
///     Enumeration controlling conversion of dynamic <see cref="XObject" /> to other types.
/// </summary>
/// <remarks>
///     You can always convert to the associated <see cref="XObject" />, regardless of this setting.
/// </remarks>
public enum ConvertMode
{
    /// <summary>
    ///     Will supply the objects `Value` string if available, otherwise will convert to a string.
    /// </summary>
    AsString,

    /// <summary>
    ///     Supports conversion to a string, as with <see cref="AsString" />, however will also attempt to convert
    ///     the value (if available) to the requested type, and if that fails, it will try to convert, the associated
    ///     <see cref="XObject" /> to the requested type.  Finally, it will return the default of the requested type if
    ///     conversion is not possible.
    /// </summary>
    ConvertOrDefault,

    /// <summary>
    ///     Supports conversion to a string, as with <see cref="AsString" />, however will also attempt to convert
    ///     the value (if available) to the requested type, and if that fails, it will try to convert, the associated
    ///     <see cref="XObject" /> to the requested type.  Finally, it will throw an <see cref="InvalidCastException" />
    ///     conversion is not possible.
    /// </summary>
    ConvertOrThrow,

    /// <summary>
    ///     The default option (<see cref="ConvertOrDefault" />)
    /// </summary>
    Default = ConvertOrDefault
}