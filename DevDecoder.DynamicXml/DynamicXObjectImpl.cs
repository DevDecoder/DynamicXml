using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

public static partial class DynamicXObject
{
    /// <summary>
    ///     The <see cref="DynamicXObjectImpl" /> extends <see cref="DynamicObject" /> to implement dynamic access to any
    ///     <see cref="XObject" />.
    /// </summary>
    /// <remarks>
    ///     We do not expose this class externally to allow us to future proof the implementation, and to prevent confusion
    ///     in consumers that might try to use the object, other than the <see langword="dynamic" /> keyword.
    /// </remarks>
    private sealed class DynamicXObjectImpl : DynamicObject
    {
        private readonly DynamicXOptions _options;
        private readonly XObject _xObject;

        /// <summary>
        ///     Create a new instance of <see cref="DynamicXObjectImpl" />, with the
        ///     <paramref name="options">specified options</paramref>
        /// </summary>
        /// <param name="xObject"></param>
        /// <param name="options"></param>
        internal DynamicXObjectImpl(XObject xObject, DynamicXOptions? options)
        {
            _xObject = xObject;
            _options = options ?? DynamicXOptions.Default;
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
            return (_xObject as XContainer)?.Elements();
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

        /// <summary>
        ///     The method info for the `Filter` method.
        /// </summary>
        private static readonly MethodInfo FilterMethod = typeof(DynamicXObject)
            .GetMethod("Filter",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                new[] {typeof(XObject), typeof(DynamicXOptions), typeof(object[])},
                null)!;

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
                        result = attributes.Select(attribute => attribute.ToDynamic(_options));
                        return true;
                    }
                }

                var elementsName = _options.GetElementsName(name);
                if (elementsName is not null)
                {
                    var elements = xElement.Elements(name);
                    if (elements.Any())
                    {
                        result = elements.Select(element => element.ToDynamic(_options));
                        return true;
                    }
                }
                // ReSharper restore PossibleMultipleEnumeration
            }

            var builtInName = _options.GetBuiltInName(binder.Name);

            if (string.Equals(builtInName, "Filter"))
            {
                // Invoke static Filter method.
                result = FilterMethod.Invoke(null, new object[] {_xObject, _options, args ?? Array.Empty<object>()});
                return true;
            }

            if (builtInName is not null)
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

            switch (_options.InvokeResultIfNotFound)
            {
                case InvokeResultIfNotFound.Empty:
                    result = Array.Empty<DynamicXObjectImpl>();
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
            // Used when index out of range
            bool Fail(Func<string> msg, out object? r)
            {
                if (_options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                    throw new IndexOutOfRangeException(msg());
                r = null;
                return true;
            }

            if (indexes.Length < 1)
                // TODO Check is this even possible?
                return Fail(() => "Must supply at least one dimension", out result);

            // Set starting object.
            result = _xObject
                .Filter(_options, indexes)
                .FirstOrDefault();
            // ReSharper restore PossibleMultipleEnumeration

            switch (result)
            {
                case XObject xObject:
                    result = xObject.ToDynamic();
                    return true;
                case null:
                    return Fail(() => "No result found", out result);
                default:
                    return true;
            }
        }

        /// <inheritdoc />
        public override string? ToString()
        {
            var value = GetInnerValue();
            return value ?? _xObject.ToString();
        }
    }
}