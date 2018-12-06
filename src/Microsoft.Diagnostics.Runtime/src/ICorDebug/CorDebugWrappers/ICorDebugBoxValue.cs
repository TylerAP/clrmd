// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.CorDebug
{
    [ComImport]
    [InterfaceType(1)]
    [Guid("CC7BCAFC-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugBoxValue : ICorDebugHeapValue
    {
        new void GetType([Out] out CorElementType pType);
        new void GetSize([Out] out uint pSize);

        new void GetAddress([Out] out ulong pAddress);

        new void CreateBreakpoint(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugValueBreakpoint ppBreakpoint);

        new void IsValid([Out] out int pbValid);

        new void CreateRelocBreakpoint(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugValueBreakpoint ppBreakpoint);

        void GetObject(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugObjectValue ppObject);
    }
}