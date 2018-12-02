using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class LUID {
        public uint LowPart;
        public int HighPart;
    }
}