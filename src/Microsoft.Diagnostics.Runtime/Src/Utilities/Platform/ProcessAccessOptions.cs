namespace Microsoft.Diagnostics.Runtime.Utilities {
    public enum ProcessAccessOptions
    {
        ProcessTerminate = 0x0001,
        ProcessCreateThread = 0x0002,
        ProcessSetSessionID = 0x0004,
        ProcessVMOperation = 0x0008,
        ProcessVMRead = 0x0010,
        ProcessVMWrite = 0x0020,
        ProcessDupHandle = 0x0040,
        ProcessCreateProcess = 0x0080,
        ProcessSetQuota = 0x0100,
        ProcessSetInformation = 0x0200,
        ProcessQueryInformation = 0x0400,
        ProcessSuspendResume = 0x0800,
        Synchronize = 0x100000
    }
}