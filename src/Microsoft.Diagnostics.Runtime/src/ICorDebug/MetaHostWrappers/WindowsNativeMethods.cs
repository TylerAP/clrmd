//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Permissions;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using static Microsoft.Diagnostics.Runtime.ICorDebug.PlatformHelper;

namespace Microsoft.Diagnostics.Runtime.ICorDebug
{
    static class WindowsNativeMethods
    {
        private const string Kernel32LibraryName = "kernel32";
        private const string Ole32LibraryName = "ole32";
        private const string ShlwapiLibraryName = "shlwapi";

        public const int MAX_PATH = 260;


        [DllImport(Kernel32LibraryName)]
        public static extern bool CloseHandle(IntPtr handle);


        [DllImport(Kernel32LibraryName, PreserveSig = true)]
        public static extern ProcessSafeHandle OpenProcess(
            Int32 dwDesiredAccess,
            bool bInheritHandle,
            Int32 dwProcessId
        );
#if false

        [DllImport(Kernel32LibraryName, CharSet = CharSet.Unicode, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageName(
            ProcessSafeHandle hProcess,
            int dwFlags,
            StringBuilder lpExeName,
            ref int lpdwSize
        );

        [DllImport(Ole32LibraryName, PreserveSig = false)]
        public static extern void CoCreateInstance(
            ref Guid rclsid,
            IntPtr pUnkOuter,
            Int32 dwClsContext,
            ref Guid riid, // must use "typeof(ICorDebug).GUID"
            [MarshalAs(UnmanagedType.Interface)] out ICorDebug debuggingInterface
        );

        public enum Stgm
        {
            StgmRead = 0x00000000,
            StgmWrite = 0x00000001,
            StgmReadWrite = 0x00000002,
            StgmShareDenyNone = 0x00000040,
            StgmShareDenyRead = 0x00000030,
            StgmShareDenyWrite = 0x00000020,
            StgmShareExclusive = 0x00000010,
            StgmPriority = 0x00040000,
            StgmCreate = 0x00001000,
            StgmConvert = 0x00020000,
            StgmFailIfThere = 0x00000000,
            StgmDirect = 0x00000000,
            StgmTransacted = 0x00010000,
            StgmNoScratch = 0x00100000,
            StgmNoSnapshot = 0x00200000,
            StgmSimple = 0x08000000,
            StgmDirectSwmr = 0x00400000,
            StgmDeleteOnRelease = 0x04000000
        }

        // SHCreateStreamOnFile* is used to create IStreams to pass to ICLRMetaHostPolicy::GetRequestedRuntime().
        // Since we can't count on the EX version being available, we have SHCreateStreamOnFile as a fallback.
        [DllImport(ShlwapiLibraryName, PreserveSig = false)]
        // Only in version 6 and later
        public static extern void SHCreateStreamOnFileEx(
            [MarshalAs(UnmanagedType.LPWStr)] string file,
            Stgm dwMode,
            Int32 dwAttributes, // Used if a file is created.  Identical to dwFlagsAndAttributes param of CreateFile.
            bool create,
            IntPtr pTemplate, // Reserved, always pass null.
            [MarshalAs(UnmanagedType.Interface)] out IStream openedStream
        );

        [DllImport(ShlwapiLibraryName, PreserveSig = false)]
        public static extern void SHCreateStreamOnFile(
            string file,
            Stgm dwMode,
            [MarshalAs(UnmanagedType.Interface)] out IStream openedStream
        );

#endif
    }
}