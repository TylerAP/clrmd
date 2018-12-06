using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime.Utilities {
    internal static class WindowsNativeMethods
    {
        internal const int MAX_PATH = 260;

        internal const uint FILE_MAP_READ = 4;

        internal const int VS_FIXEDFILEINFO_size = 0x34;

        internal const short IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR = 14;

        private const string Kernel32LibraryName = "kernel32";
        //private const string Ole32LibraryName = "ole32";
        //private const string ShlwapiLibraryName = "shlwapi";
        private const string AdvApi32LibraryName = "advapi32";
        private const string VersionLibraryName = "version";
        private const string PsapiLibraryName = "psapi";
        private const string DbgEngLibraryName = "dbgeng";

        [DllImport(Kernel32LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        internal static IntPtr LoadLibrary(string lpFileName) =>
            LoadLibraryEx(lpFileName, 0, LoadLibraryFlags.NoFlags);

        [DllImport(Kernel32LibraryName, SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx(string fileName, int hFile, LoadLibraryFlags dwFlags);

        [DllImport(Kernel32LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool isWow64);

        [DllImport(VersionLibraryName)]
        internal static extern bool GetFileVersionInfo(string sFileName, int handle, int size, byte[] infoBuffer);

        [DllImport(VersionLibraryName)]
        internal static extern int GetFileVersionInfoSize(string sFileName, out int handle);

        [DllImport(VersionLibraryName)]
        internal static extern bool VerQueryValue(byte[] pBlock, string pSubBlock, out IntPtr val, out int len);

        [DllImport(Kernel32LibraryName)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport(PsapiLibraryName, SetLastError = true)]
        internal static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb, [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

        [DllImport(PsapiLibraryName, SetLastError = true)]
        [PreserveSig]
        internal static extern uint GetModuleFileNameExA([In] IntPtr hProcess, [In] IntPtr hModule, [Out] StringBuilder lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport(Kernel32LibraryName)]
        internal static extern int ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
            byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead);

        [DllImport(Kernel32LibraryName, SetLastError = true)]
        internal static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

        [DllImport(Kernel32LibraryName)]
        internal static extern bool GetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport(Kernel32LibraryName, SetLastError = true)]
        internal static extern SafeWin32Handle OpenThread(ThreadAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

        [DllImport(Kernel32LibraryName)]
        internal static extern int PssCaptureSnapshot(IntPtr processHandle, PSS_CAPTURE_FLAGS captureFlags, int threadContextFlags, out IntPtr snapshotHandle);

        [DllImport(Kernel32LibraryName)]
        internal static extern int PssFreeSnapshot(IntPtr processHandle, IntPtr snapshotHandle);

        [DllImport(Kernel32LibraryName)]
        internal static extern int PssQuerySnapshot(IntPtr snapshotHandle, PSS_QUERY_INFORMATION_CLASS informationClass, out IntPtr buffer, int bufferLength);

        [DllImport(Kernel32LibraryName)]
        internal static extern int GetProcessId(IntPtr hObject);

        [DllImport(Kernel32LibraryName)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out int lpNumberOfBytesRead);


        // Call CloseHandle to clean up.
        [DllImport(Kernel32LibraryName, SetLastError = true)]
        internal static extern SafeWin32Handle CreateFileMapping(
            SafeFileHandle hFile,
            IntPtr lpFileMappingAttributes,
            PageProtection flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName);

        [DllImport(Kernel32LibraryName, SetLastError = true)]
        internal static extern SafeMapViewHandle MapViewOfFile(
            SafeWin32Handle hFileMappingObject,
            uint
                dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            IntPtr dwNumberOfBytesToMap);

        [DllImport(Kernel32LibraryName)]
        internal static extern void RtlMoveMemory(IntPtr destination, IntPtr source, IntPtr numberBytes);

        [DllImport(Kernel32LibraryName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnmapViewOfFile(IntPtr baseAddress);


        [DefaultDllImportSearchPaths(DllImportSearchPath.LegacyBehavior)]
        [DllImport(DbgEngLibraryName)]
        public static extern uint DebugCreate(ref Guid InterfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object Interface);
        
        [DllImport(Kernel32LibraryName, PreserveSig = true)]
        internal static extern ProcessSafeHandle OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport(Kernel32LibraryName)]
        internal static extern bool CloseHandle(IntPtr handle);

#if false
        [DllImport(Kernel32LibraryName, CharSet = CharSet.Unicode, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryFullProcessImageName(
            ProcessSafeHandle hProcess,
            int dwFlags,
            StringBuilder lpExeName,
            ref int lpdwSize
        );

        [DllImport(Ole32LibraryName, PreserveSig = false)]
        internal static extern void CoCreateInstance(
            ref Guid rclsid,
            IntPtr pUnkOuter,
            Int32 dwClsContext,
            ref Guid riid, // must use "typeof(ICorDebug).GUID"
            [MarshalAs(UnmanagedType.Interface)] out ICorDebug debuggingInterface
        );

        internal enum Stgm
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
        internal static extern void SHCreateStreamOnFileEx(
            [MarshalAs(UnmanagedType.LPWStr)] string file,
            Stgm dwMode,
            Int32 dwAttributes, // Used if a file is created.  Identical to dwFlagsAndAttributes param of CreateFile.
            bool create,
            IntPtr pTemplate, // Reserved, always pass null.
            [MarshalAs(UnmanagedType.Interface)] out IStream openedStream
        );

        [DllImport(ShlwapiLibraryName, PreserveSig = false)]
        internal static extern void SHCreateStreamOnFile(
            string file,
            Stgm dwMode,
            [MarshalAs(UnmanagedType.Interface)] out IStream openedStream
        );

#endif

        [DllImport(AdvApi32LibraryName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(
            IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
            ref TOKEN_PRIVILEGES newState,
            uint bufferLengthInBytes,
            ref TOKEN_PRIVILEGES previousState,
            out uint returnLengthInBytes
        );
    }
}