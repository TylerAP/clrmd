// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.CorDebug
{
    [ComImport]
    [InterfaceType(1)]
    [Guid("6164D242-1015-4BD6-8CBE-D0DBD4B8275A")]
    public interface ICorDebugGuidToTypeEnum : ICorDebugEnum
    {
        new void Skip([In] uint celt);

        new void Reset();

        new void Clone(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next(
            [In] uint celt,
            [Out][MarshalAs(UnmanagedType.LPArray)]
            CorDebugGuidToTypeMapping[] values,
            [Out] out uint pceltFetched);
    }
}