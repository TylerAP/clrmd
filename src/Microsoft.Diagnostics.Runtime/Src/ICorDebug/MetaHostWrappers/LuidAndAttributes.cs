using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class LUID_AND_ATTRIBUTES {
        public LUID Luid;
        public uint Attributes;
    }
}