// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static Microsoft.Diagnostics.Runtime.Utilities.WindowsNativeMethods;

#pragma warning disable 1591

namespace Microsoft.Diagnostics.Runtime
{
    internal class ProcessSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal ProcessSafeHandle()
            : base(true)
        {
        }

        internal ProcessSafeHandle(IntPtr handle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(handle);
        }

        internal bool IsNull()
        {
            return handle == default;
        }

        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }

        internal static ProcessSafeHandle FromSafeHandle(SafeHandle h)
        {
            return new ProcessSafeHandle(h.DangerousGetHandle(), false);
        }
    }
}