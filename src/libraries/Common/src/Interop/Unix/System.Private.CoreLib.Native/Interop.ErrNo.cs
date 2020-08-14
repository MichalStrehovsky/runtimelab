// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [DllImport(Interop.Libraries.CoreLibNative, EntryPoint = "CoreLibNative_GetErrNo")]
        internal static extern int GetErrNo();

        [DllImport(Interop.Libraries.CoreLibNative, EntryPoint = "CoreLibNative_ClearErrNo")]
        internal static extern void ClearErrNo();
    }
}