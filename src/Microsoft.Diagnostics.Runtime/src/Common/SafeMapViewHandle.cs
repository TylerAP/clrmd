﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static Microsoft.Diagnostics.Runtime.Utilities.WindowsNativeMethods;

namespace Microsoft.Diagnostics.Runtime
{
    internal sealed class SafeMapViewHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeMapViewHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return UnmapViewOfFile(handle);
        }

        // This is technically equivalent to DangerousGetHandle, but it's safer for file
        // mappings. In file mappings, the "handle" is actually a base address that needs
        // to be used in computations and RVAs.
        // So provide a safer accessor method.
        public IntPtr BaseAddress => handle;
    }
}