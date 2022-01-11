using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace DevDecoder.DynamicXml;

public abstract class DynamicXObject : DynamicObject
{
    protected readonly DynamicXOptions Options;
    protected readonly XObject XObject;

    protected DynamicXObject(XObject xObject, DynamicXOptions? options)
    {
        XObject = xObject;
        Options = options ?? DynamicXOptions.Default;
    }

    /// <inheritdoc />
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var name = Options.GetBuiltInName(binder.Name);
        if (name is not null)
            try
            {
                switch (XObject.GetType().GetMember(name).FirstOrDefault())
                {
                    case PropertyInfo propertyInfo:
                        result = propertyInfo.GetValue(XObject);
                        return true;
                    case FieldInfo fieldInfo:
                        result = fieldInfo.GetValue(XObject);
                        return true;
                }
            }
            catch
            {
                // ignored
            }

        if (Options.PropertyResultIfNotFound == PropertyResultIfNotFound.Throw)
            throw new InvalidOperationException($"Cannot invoke '{binder.Name}', not found!");

        result = null;
        return true;
    }

    /// <inheritdoc />
    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        var name = Options.GetBuiltInName(binder.Name);
        if (name is not null)
            try
            {
                result = XObject.GetType().InvokeMember(
                    name,
                    BindingFlags.InvokeMethod,
                    null,
                    XObject,
                    args);
                return true;
            }
            catch
            {
                // ignored
            }

        switch (Options.InvokeResultIfNotFound)
        {
            case InvokeResultIfNotFound.Empty:
                result = Array.Empty<DynamicXElement>();
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
        if (binder.Type.IsInstanceOfType(XObject))
        {
            result = XObject;
            return true;
        }

        if (Options.ConvertMode == ConvertMode.ValueString)
        {
            // Value strings use other overload when available.
            result = null;
            return true;
        }

        try
        {
            result = Convert.ChangeType(XObject, binder.Type);
            return true;
        }
        catch
        {
            // ignored
        }

        if (Options.ConvertMode == ConvertMode.ConvertOrThrow)
            throw new InvalidCastException($"Could not cast XML object to {binder.Type}");

        // Return default of requested type.
        result = binder.Type.IsValueType ? Activator.CreateInstance(binder.Type) : null;
        return true;
    }

    protected virtual bool TryConvert(string innerValue, ConvertBinder binder, out object? result)
    {
        // Can always convert to the underlying XObject.
        if (binder.Type.IsInstanceOfType(XObject))
        {
            result = XObject;
            return true;
        }

        if (Options.ConvertMode == ConvertMode.ValueString || binder.Type == typeof(string))
        {
            result = innerValue;
            return true;
        }

        // Try to convert inner value first
        try
        {
            result = Convert.ChangeType(innerValue, binder.Type);
            return true;
        }
        catch
        {
            // ignored
        }

        // Try to convert XObject
        try
        {
            result = Convert.ChangeType(XObject, binder.Type);
            return true;
        }
        catch
        {
            // ignored
        }

        if (Options.ConvertMode == ConvertMode.ConvertOrThrow)
            throw new InvalidCastException($"Could not cast XML object to {binder.Type}");

        // Return default of requested type.
        result = binder.Type.IsValueType ? Activator.CreateInstance(binder.Type) : null;
        return true;
    }

    protected bool TryGetIndex(IEnumerable<DynamicXElement> items, object[] indexes, out object? result)
    {
        if (indexes.Length != 1)
        {
            if (Options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException("Multiple dimensions not supported");

            result = Array.Empty<DynamicXElement>();
            return true;
        }

        var iObj = indexes[0];
        if (!(iObj is IConvertible convertible))
        {
            if (Options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException("Indexer type must be convertible to long.");

            result = Array.Empty<DynamicXElement>();
            return true;
        }

        var index = convertible.ToInt64(null);
        if (index < 0)
        {
            if (Options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException($"Index out of range {index}");
            result = Array.Empty<DynamicXElement>();
            return true;
        }

        using var enumerator = items.GetEnumerator();
        var counter = index;
        do
        {
            counter--;
            if (enumerator.MoveNext()) continue;

            if (Options.IndexResultIfNotFound == IndexResultIfNotFound.Throw)
                throw new IndexOutOfRangeException($"Index out of range {index}");
            result = Array.Empty<DynamicXElement>();
            return true;
        } while (counter >= 0);

        result = enumerator.Current;
        return true;
    }

    public static implicit operator XObject(DynamicXObject xObject)
    {
        return xObject.XObject;
    }
}

public sealed class DynamicXDocument : DynamicXObject
{
    public DynamicXDocument(XDocument document, DynamicXOptions? options = null)
        : base(document, options)
    {
    }

    /// <inheritdoc />
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var document = (XDocument) XObject;
        if (document.Root is not null &&
            document.Root.Name == Options.GetElementXName(document.Root.GetDefaultNamespace(), binder.Name))
        {
            // Expose Root property
            result = new DynamicXElement(document.Root, Options);
            return true;
        }

        return base.TryGetMember(binder, out result);
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetDynamicMemberNames()
    {
        if (!Options.ExposeElement) yield break;
        var document = (XDocument) XObject;
        if (document.Root is null) yield break;
        yield return Options.GetName(document.Root.Name);
    }

    /// <inheritdoc />
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        var root = ((XDocument) XObject).Root;
        return TryGetIndex(
            root is null
                ? Enumerable.Empty<DynamicXElement>()
                : new[] {new DynamicXElement(root, Options)},
            indexes, out result);
    }

    public static implicit operator XDocument(DynamicXDocument xDocument)
    {
        return (XDocument) xDocument.XObject;
    }
}

public sealed class DynamicXAttribute : DynamicXObject
{
    public DynamicXAttribute(XAttribute xObject, DynamicXOptions? options = null)
        : base(xObject, options)
    {
    }

    public override bool TryConvert(ConvertBinder binder, out object? result)
    {
        return TryConvert(((XAttribute) XObject).Value, binder, out result);
    }

    /// <inheritdoc />
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        return TryGetIndex(Array.Empty<DynamicXElement>(), indexes, out result);
    }

    public static implicit operator XAttribute(DynamicXAttribute xDocument)
    {
        return (XAttribute) xDocument.XObject;
    }
}

public sealed class DynamicXElement : DynamicXObject
{
    public DynamicXElement(XElement element, DynamicXOptions? options = null)
        : base(element, options)
    {
    }

    /// <inheritdoc />
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        var element = (XElement) XObject;
        var name = Options.GetAttributeXName(element.GetDefaultNamespace(), binder.Name);
        if (name is not null)
        {
            var attribute = element.Attribute(name);
            if (attribute is not null)
            {
                result = new DynamicXAttribute(attribute, Options);
                return true;
            }
        }

        name = Options.GetElementXName(element.GetDefaultNamespace(), binder.Name);
        if (name is not null)
        {
            var el = element.Element(name);
            if (el is not null)
            {
                result = new DynamicXElement(el, Options);
                return true;
            }
        }

        return base.TryGetMember(binder, out result);
    }

    /// <inheritdoc />
    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        if (binder.CallInfo.ArgumentCount == 0)
        {
            var element = (XElement) XObject;
            var name = Options.GetAttributesXName(element.GetDefaultNamespace(), binder.Name);
            // ReSharper disable PossibleMultipleEnumeration
            if (name is not null)
            {
                var attributes = element.Attributes(name);
                if (attributes.Any())
                {
                    result = attributes.Select(attribute => new DynamicXAttribute(attribute, Options));
                    return true;
                }
            }

            name = Options.GetElementsXName(element.GetDefaultNamespace(), binder.Name);
            if (name is not null)
            {
                var el = element.Elements(name);
                if (el.Any())
                {
                    result = el.Select(e => new DynamicXElement(e, Options));
                    return true;
                }
            }
            // ReSharper restore PossibleMultipleEnumeration
        }

        return base.TryInvokeMember(binder, args, out result);
    }

    /// <inheritdoc />
    public override bool TryConvert(ConvertBinder binder, out object? result)
    {
        return TryConvert(((XElement) XObject).Value, binder, out result);
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetDynamicMemberNames()
    {
        var seen = new HashSet<string>();
        var element = (XElement) XObject;
        if (Options.ExposeAttributes)
            foreach (var attribute in element.Attributes())
            {
                var name = Options.AttributePrefix + Options.GetName(attribute.Name);
                if (seen.Add(name))
                    yield return name;
            }

        if (Options.ExposeElement)
            foreach (var el in element.Elements())
            {
                var name = Options.ElementPrefix + Options.GetName(el.Name);
                if (seen.Add(name))
                    yield return name;
            }
    }

    /// <inheritdoc />
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        return TryGetIndex(
            ((XElement) XObject).Elements().Select(el => new DynamicXElement(el, Options)),
            indexes,
            out result);
    }

    public static implicit operator XElement(DynamicXElement xElement)
    {
        return (XElement) xElement.XObject;
    }
}