using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

public abstract class DynamicXObject : DynamicObject
{
    protected readonly XObject XObject;
    protected readonly DynamicXOptions Options;

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

        if (Options.ThrowIfNotFound)
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

        if (Options.ThrowIfNotFound)
            throw new InvalidOperationException($"Cannot invoke '{binder.Name}', not found!");

        result = null;
        return true;
    }

    /// <inheritdoc />
    public override bool TryConvert(ConvertBinder binder, out object? result)
    {
        if (binder.Type.IsInstanceOfType(XObject))
        {
            result = XObject;
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

        if (Options.ThrowInvalidCast)
            throw new InvalidCastException($"Could not cast XML object to {binder.Type}");

        // Return default of requested type.
        result = binder.Type.IsValueType ? Activator.CreateInstance(binder.Type) : null;
        return true;
    }
    
    public static implicit operator XObject(DynamicXObject xObject) => xObject.XObject;
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
            result = new DynamicXElement(this, document.Root, Options);
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
    
    public static implicit operator XDocument(DynamicXDocument xDocument) => (XDocument)xDocument.XObject;
}

public sealed class DynamicXAttribute : DynamicXObject
{
    private readonly DynamicXElement _parent;

    public DynamicXAttribute(DynamicXElement parent, XAttribute xObject, DynamicXOptions? options = null)
        : base(xObject, options) => _parent = parent;

    public override bool TryConvert(ConvertBinder binder, out object? result)
    {
        try
        {
            result = Convert.ChangeType(((XAttribute) XObject).Value, binder.Type);
            return true;
        }
        catch
        {
            // ignored
        }

        return base.TryConvert(binder, out result);
    }
    
    public static implicit operator XAttribute(DynamicXAttribute xDocument) => (XAttribute)xDocument.XObject;
}

public sealed class DynamicXElement : DynamicXObject
{
    private readonly DynamicXObject _parent;

    public DynamicXElement(DynamicXObject parent, XElement element, DynamicXOptions? options = null)
        : base(element, options) =>
        _parent = parent;

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
                result = new DynamicXAttribute(this, attribute, Options);
                return true;
            }
        }

        name = Options.GetElementXName(element.GetDefaultNamespace(), binder.Name);
        if (name is not null)
        {
            var el = element.Element(name);
            if (el is not null)
            {
                result = new DynamicXElement(this, el, Options);
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
            if (name is not null)
            {
                var attributes = element.Attributes(name);
                if (attributes.Any())
                {
                    result = attributes.Select(attribute => new DynamicXAttribute(this, attribute, Options));
                    return true;
                }
            }

            name = Options.GetElementsXName(element.GetDefaultNamespace(), binder.Name);
            if (name is not null)
            {
                var el = element.Elements(name);
                if (el.Any())
                {
                    result = el.Select(e => new DynamicXElement(this, e, Options));
                    return true;
                }
            }
        }

        return base.TryInvokeMember(binder, args, out result);
    }

    /// <inheritdoc />
    public override bool TryConvert(ConvertBinder binder, out object? result)
    {
        try
        {
            result = Convert.ChangeType(((XElement) XObject).Value, binder.Type);
            return true;
        }
        catch
        {
            // ignored
        }

        return base.TryConvert(binder, out result);
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetDynamicMemberNames()
    {
        var seen = new HashSet<string>();
        var element = (XElement) XObject;
        if (Options.ExposeAttributes)
        {
            foreach (var attribute in element.Attributes())
            {
                var name = Options.AttributePrefix + Options.GetName(attribute.Name);
                if (seen.Add(name))
                    yield return name;
            }
        }

        if (Options.ExposeElement)
        {
            foreach (var el in element.Elements())
            {
                var name = Options.ElementPrefix + Options.GetName(el.Name);
                if (seen.Add(name))
                    yield return name;
            }
        }
    }

    /// <inheritdoc />
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        if (indexes.Length != 1)
        {
            result = null;
            return false;
        }

        result = new DynamicXElement(this, ((XElement) XObject).Elements().Skip((int) indexes[0]).First(), Options);
        return true;
    }
    
    public static implicit operator XElement(DynamicXElement xElement) => (XElement)xElement.XObject;
}