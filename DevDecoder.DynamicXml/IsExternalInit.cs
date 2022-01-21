﻿using System.ComponentModel;

// SHIM To allow record support in .NET Standard
#if NETSTANDARD2_1
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IsExternalInit
    {
    }
}
#endif