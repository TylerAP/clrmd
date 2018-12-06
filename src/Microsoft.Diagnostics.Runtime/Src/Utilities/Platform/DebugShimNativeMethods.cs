using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.CorDebug;

namespace Microsoft.Diagnostics.Runtime.Utilities {
    internal static class DebugShimNativeMethods
    {
        private const string DbgShimLibraryName = "dbgshim"; //"mscoree";
        
        [DllImport(DbgShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern IntPtr CreateDebuggingInterfaceFromVersion(
            [MarshalAs(UnmanagedType.I4)]
            CorDebugVersion iDebuggerVersion,
            [MarshalAs(UnmanagedType.LPWStr)]
            string szDebuggeeVersion,
            out IntPtr pCorDb);

        [DllImport(DbgShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void GetVersionFromProcess(
            ProcessSafeHandle hProcess,
            StringBuilder versionString,
            int bufferSize,
            out int dwLength);

        [DllImport(DbgShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void GetRequestedRuntimeVersion(
            string pExe,
            StringBuilder pVersion,
            int cchBuffer,
            out int dwLength);

        [DllImport(DbgShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = true)]
        internal static extern IntPtr CLRCreateInstance(
            ref Guid clsid,
            ref Guid riid,
            out IntPtr metaHostInterface);

        [DllImport(DbgShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false, EntryPoint = "CLRCreateInstance")]
        [return: MarshalAs(UnmanagedType.Interface)]
        internal static extern object CLRCreateInstance2(
            ref Guid clsid,
            ref Guid riid);

        
    }
}