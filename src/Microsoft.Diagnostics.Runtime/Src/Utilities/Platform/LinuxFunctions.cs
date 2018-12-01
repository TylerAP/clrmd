using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime
{
    internal sealed class LinuxFunctions : PlatformFunctions
    {
        public override bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
        {
            //TODO

            major = minor = revision = patch = 0;
            return true;
        }

        public override bool TryGetWow64(IntPtr proc, out bool result)
        {
            result = false;
            return true;
        }

        public override IntPtr LoadLibrary(string filename)
        {
            try
            {
                return V2.dlopen(filename, RTLD_NOW);
            }
            catch (DllNotFoundException)
            {
                return V1.dlopen(filename, RTLD_NOW);
            }
        }

        public override bool FreeLibrary(IntPtr module)
        {
            try
            {
                if (V2.dlclose(module) == 0)
                    return true;
                return false;
            }
            catch (DllNotFoundException)
            {
                if (V1.dlclose(module) == 0)
                    return true;
                return false;
            }
        }

        public override IntPtr GetProcAddress(IntPtr module, string method)
        {
            try
            {
                return V2.dlsym(module, method);
            }
            catch (DllNotFoundException)
            {
                return V1.dlsym(module, method);
            }
        }


        const int RTLD_NOW = 2;
        
        internal static class V1
        {

            [DllImport("dl")]
            internal static extern IntPtr dlopen(string filename, int flags);

            [DllImport("dl")]
            internal static extern int dlclose(IntPtr module);

            [DllImport("dl")]
            internal static extern IntPtr dlsym(IntPtr handle, string symbol);

        }
        
        internal static class V2
        {

            [DllImport("libdl.so.2")]
            internal static extern IntPtr dlopen(string filename, int flags);

            [DllImport("libdl.so.2")]
            internal static extern int dlclose(IntPtr module);

            [DllImport("libdl.so.2")]
            internal static extern IntPtr dlsym(IntPtr handle, string symbol);

        }
    }
}
