using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

/// <summary>
/// The <see cref="DynamicXObject"/> extends <see cref="DynamicObject"/> to implement dynamic access to any
/// <see cref="XObject"/>.
/// </summary>
internal sealed class DynamicXObject : DynamicObject
{
    private readonly DynamicXmlOptions _options;
    private readonly XObject _xObject;

    /// <summary>
    /// Create a new instance of <see cref="DynamicXObject"/>, with the <paramref name="options">specified options</paramref>
    /// </summary>
    /// <param name="xObject"></param>
    /// <param name="options"></param>
    internal DynamicXObject(XObject xObject, DynamicXmlOptions? options)
    {
        _xObject = xObject;
        _options = options ?? DynamicXmlOptions.Default;
    }

    /// <summary>
    ///     Gets the string value of the <see cref="_xObject" />, if any.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string? GetInnerValue()
    {
        return _xObject switch
        {
            XElement xElement => xElement.Value,
            XAttribute xAttribute => xAttribute.Value,
            XComment xComment => xComment.Value,
            XText xText => xText.Value,
            _ => null
        };
    }

    /// <summary>
    ///     Gets the child elements, if any.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<XElement>? GetElements()
    {
        return _xObject switch
        {
            XDocument xDocument => xDocument.Root is null
                ? null
                : Enumerable.Repeat(xDocument.Root, 1),
            XElement xElement => xElement.Elements(),
            _ => null
        };
    }

    /// <summary>
    ///     Gets the child elements, if any.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<XAttribute>? GetAttributes()
    {
        return _xObject switch
        {
            XElement xElement => xElement.Attributes(),
            _ => null
        };
    }

    /// <inheritdoc />
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var name = binder.Name;
        var attributes = GetAttributes();
        if (attributes is not null)
        {
            var attributeName = _options.GetAttributeName(name);
            if (attributeName is not null)
            {
                result = attributes
                    .FirstOrDefault(
                        attribute => _options.AttributeComparer.Equals(attribute.Name.LocalName, attributeName))
                    .ToDynamic();
                if (result is not null) return true;
            }
        }

        var elements = GetElements();
        if (elements is not null)
        {
            var elementName = _options.GetElementName(name);
            if (elementName is not null)
            {
                result = elements
                    .FirstOrDefault(
                        element => _options.ElementComparer.Equals(element.Name.LocalName, elementName))
                    .ToDynamic();
                if (result is not null) return true;
            }
        }

        var builtInName = _options.GetBuiltInName(binder.Name);
        if (builtInName is not null)
            try
            {
                switch (_xObject.GetType().GetMember(name).FirstOrDefault())
                {
                    case PropertyInfo propertyInfo:
                        result = propertyInfo.GetValue(_xObject);
                        return true;
                    case FieldInfo fieldInfo:
                        result = fieldInfo.GetValue(_xObject);
                        return true;
                }
            }
            catch
            {
                // ignored
            }

        if (_options.PropertyResultIfNotFound == PropertyResultIfNotFound.Throw)
            throw new InvalidOperationException($"Cannot invoke '{binder.Name}', not found!");

        result = null;
        return true;
    }

    /// <inheritdoc />
    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        var name = binder.Name;
        if (_options.ExposeEnums &&
            binder.CallInfo.ArgumentCount == 0 &&
            _xObject is XElement xElement)
        {
            var attributesName = _options.GetAttributesName(name);
            // ReSharper disable PossibleMultipleEnumeration
            if (attributesName is not null)
            {
                var attributes = xElement.Attributes(name);
                if (attributes.Any())
                {
                    result = attributes.Select(attribute => new DynamicXObject(attribute, _options));
                    return true;
                }
            }

            var elementsName = _options.GetElementsName(name);
            if (elementsName is not null)
            {
                var elements = xElement.Elements(name);
                if (elements.Any())
                {
                    result = elements.Select(element => new DynamicXObject(element, _options));
                    return true;
                }
            }
            // ReSharper restore PossibleMultipleEnumeration
        }

        var builtInName = _options.GetBuiltInName(binder.Name);
        if (builtInName is not null)
        {
            try
            {
                result = _xObject.GetType().InvokeMember(
                    builtInName,
                    BindingFlags.InvokeMethod,
                    null,
                    _xObject,
                    args);
                return true;
            }
            catch
            {
                // ignored
            }
        }

        switch (_options.InvokeResultIfNotFound)
        {
            case InvokeResultIfNotFound.Empty:
                result = Array.Empty<DynamicXObject>();
                return true;
            case InvokeResultIfNotFound.Throw:
                throw new InvalidOperationException($"Cannot invoke '{binder.Name}', not found!");
            case InvokeResultIfNotFound.Null:
            default:
                result = null;
                return true;
        }
    }

    /// <inheritdoc />
    public override bool TryConvert(ConvertBinder binder, out object? result)
    {
        // Can always convert to the underlying XObject.
        if (binder.Type.IsInstanceOfType(_xObject))
        {
            result = _xObject;
            return true;
        }

        var innerValue = GetInnerValue();
        if (innerValue is not null)
        {
            // Try to convert inner value first
            if (_options.ConvertMode == ConvertMode.AsString || binder.Type == typeof(string))
            {
                result = innerValue;
                return true;
            }

            try
            {
                result = Convert.ChangeType(innerValue, binder.Type);
                return true;
            }
            catch
            {
                // ignored
            }
        }

        // Try to convert XObject
        if (_options.ConvertMode == ConvertMode.AsString || binder.Type == typeof(string))
        {
            result = _xObject.ToString();
            return true;
        }

        try
        {
            result = Convert.ChangeType(_xObject, binder.Type);
            return true;
        }
        catch
        {
            // ignored
        }

        if (_options.ConvertMode == ConvertMode.ConvertOrThrow)
            throw new InvalidCastException($"Could not cast XML object to {binder.Type}");

        // Return default of requested type.
        result = binder.Type.IsValueType ? Activator.CreateInstance(binder.Type) : null;
        return true;
    }

    /// <inheritdoc />
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        var items = GetElements();
        if (items is null)
        {
            if (_options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException("No child items");

            result = null;
            return true;
        }

        if (indexes.Length != 1)
        {
            if (_options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException("Multiple dimensions not supported");

            result = null;
            return true;
        }

        var iObj = indexes[0];
        if (!(iObj is IConvertible convertible))
        {
            if (_options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException("Indexer type must be convertible to long");

            result = null;
            return true;
        }

        var index = convertible.ToInt64(null);
        if (index < 0)
        {
            if (_options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException($"Index out of range {index}");

            result = null;
            return true;
        }

        using var enumerator = items.GetEnumerator();
        var counter = index;
        do
        {
            counter--;
            if (enumerator.MoveNext()) continue;

            if (_options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException($"Index out of range {index}");

            result = null;
            return true;
        } while (counter >= 0);

        result = enumerator.Current.ToDynamic();
        return true;
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        var value = GetInnerValue();
        return value ?? _xObject.ToString();
    }
}