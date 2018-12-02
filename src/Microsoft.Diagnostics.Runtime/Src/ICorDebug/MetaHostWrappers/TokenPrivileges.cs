using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class TOKEN_PRIVILEGES {
        public int PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }
}