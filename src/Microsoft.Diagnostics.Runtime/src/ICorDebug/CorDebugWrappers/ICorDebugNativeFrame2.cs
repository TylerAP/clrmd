// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.CorDebug
{
    [ComImport]
    [InterfaceType(1)]
    [Guid("35389FF1-3684-4c55-A2EE-210F26C60E5E")]
    public interface ICorDebugNativeFrame2
    {
        void IsChild([Out] out int pChild);

        void IsMatchingParentFrame(
            [In][MarshalAs(UnmanagedType.Interface)]
            ICorDebugNativeFrame2 pFrame,
            [Out] out int pParent);

        void GetCalleeStackParameterSize([Out] out uint pSize);
    }
}