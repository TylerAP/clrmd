// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using static Microsoft.Diagnostics.Runtime.Utilities.WindowsNativeMethods;

namespace Microsoft.Diagnostics.Runtime.Utilities
{
    internal sealed class WindowsFunctions : PlatformFunctions
    {
        public override bool FreeLibrary(IntPtr module)
        {
            return WindowsNativeMethods.FreeLibrary(module);
        }

        public override bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
        {
            major = minor = revision = patch = 0;

            int len = GetFileVersionInfoSize(dll, out int handle);
            if (len <= 0)
                return false;

            byte[] data = new byte[len];
            if (!GetFileVersionInfo(dll, handle, len, data))
                return false;

            if (!VerQueryValue(data, "\\", out IntPtr ptr, out len))
                return false;

            byte[] vsFixedInfo = new byte[len];
            Marshal.Copy(ptr, vsFixedInfo, 0, len);

            minor = (ushort)Marshal.ReadInt16(vsFixedInfo, 8);
            major = (ushort)Marshal.ReadInt16(vsFixedInfo, 10);
            patch = (ushort)Marshal.ReadInt16(vsFixedInfo, 12);
            revision = (ushort)Marshal.ReadInt16(vsFixedInfo, 14);

            return true;
        }

        public override IntPtr GetProcAddress(IntPtr module, string method)
        {
            return WindowsNativeMethods.GetProcAddress(module, method);
        }

        public override IntPtr LoadLibrary(string lpFileName)
        {
            return LoadLibraryEx(lpFileName, 0, LoadLibraryFlags.NoFlags);
        }

        public override bool TryGetWow64(IntPtr proc, out bool result)
        {
            if (Environment.OSVersion.Version.Major > 5 ||
                Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1)
            {
                return IsWow64Process(proc, out result);
            }

            result = false;
            return false;
        }
    }
}