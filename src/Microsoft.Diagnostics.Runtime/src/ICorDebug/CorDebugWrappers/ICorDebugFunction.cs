// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.CorDebug
{
    [ComImport]
    [Guid("CC7BCAF3-8A68-11D2-983C-0000F808342D")]
    [InterfaceType(1)]
    public interface ICorDebugFunction
    {
        void GetModule(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugModule ppModule);

        void GetClass(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugClass ppClass);

        void GetToken([Out] out uint pMethodDef);

        void GetILCode(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugCode ppCode);

        void GetNativeCode(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugCode ppCode);

        void CreateBreakpoint(
            [Out][MarshalAs(UnmanagedType.Interface)]
            out ICorDebugFunctionBreakpoint ppBreakpoint);

        void GetLocalVarSigToken([Out] out uint pmdSig);

        void GetCurrentVersionNumber([Out] out uint pnCurrentVersion);
    }
}