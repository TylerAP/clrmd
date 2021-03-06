// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.CorDebug
{
    [ComImport]
    [Guid("4A2A1EC9-85EC-4BFB-9F15-A89FDFE0FE83")]
    [ComConversionLoss]
    [InterfaceType(1)]
    public interface ICorDebugAssemblyEnum : ICorDebugEnum
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
            ICorDebugAssembly[] values,
            [Out] out uint pceltFetched);
    }
}