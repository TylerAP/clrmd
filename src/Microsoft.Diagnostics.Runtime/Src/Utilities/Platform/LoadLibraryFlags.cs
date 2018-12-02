using System;

namespace Microsoft.Diagnostics.Runtime.Utilities {
    [Flags]
    public enum LoadLibraryFlags : uint
    {
        NoFlags = 0x00000000,
        DontResolveDllReferences = 0x00000001,
        LoadIgnoreCodeAuthzLevel = 0x00000010,
        LoadLibraryAsDatafile = 0x00000002,
        LoadLibraryAsDatafileExclusive = 0x00000040,
        LoadLibraryAsImageResource = 0x00000020,
        LoadWithAlteredSearchPath = 0x00000008
    }
}