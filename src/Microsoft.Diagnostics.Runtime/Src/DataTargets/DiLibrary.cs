using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.CorDebug;

namespace Microsoft.Diagnostics.Runtime {
    public sealed class DiLibrary : IDisposable
    {
        /* STDAPI CoreCLRCreateCordbObject(int iDebuggerVersion, DWORD pid, HMODULE hmodTargetCLR, IUnknown ** ppCordb) */

        
        public void Dispose() { }
    }

    public static class DiLibraryNative
    {
        private const string MsCorDbIFileName = "mscordbi";

        [DllImport(MsCorDbIFileName)]
        public static extern void CoreCLRCreateCordbObject(CorDebugVersion iDebuggerVersion, uint pid, IntPtr hModTargetClr, out  /*ICorDebug*/ IntPtr pCorDb);
        
    }
}