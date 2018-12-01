using System;
using System.IO;
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
            IntPtr h;
            
            try
            {
                h = V2.dlopen(filename, RTLD_NOW | RTLD_GLOBAL);
            }
            catch (DllNotFoundException)
            {
                h = V1.dlopen(filename, RTLD_NOW | RTLD_GLOBAL);
            }

            if (h != default)
                return h;

            string m;
            try
            {
                var p = V2.dlerror();
                m = p == default ? "Unknown error." : Marshal.PtrToStringAnsi(p);
            }
            catch (DllNotFoundException)
            {
                var p = V1.dlerror();
                m = p == default ? "Unknown error." : Marshal.PtrToStringAnsi(p);
            }
            throw new InvalidOperationException($"Error loading library {filename}", new Exception(m));

        }

        public override bool FreeLibrary(IntPtr module)
        {
            try
            {
                return V2.dlclose(module) == 0;
            }
            catch (DllNotFoundException)
            {
                return V1.dlclose(module) == 0;
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


        //const int RTLD_LOCAL  = 0x000;
        //const int RTLD_LAZY   = 0x001;
        const int RTLD_NOW    = 0x002;
        const int RTLD_GLOBAL = 0x100;
        
        internal static class V1
        {

            [DllImport("dl")]
            internal static extern IntPtr dlopen(string filename, int flags);

            [DllImport("dl")]
            internal static extern int dlclose(IntPtr module);

            [DllImport("dl")]
            internal static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("dl")]
            internal static extern IntPtr dlerror();

        }
        
        internal static class V2
        {

            [DllImport("libdl.so.2")]
            internal static extern IntPtr dlopen(string filename, int flags);

            [DllImport("libdl.so.2")]
            internal static extern int dlclose(IntPtr module);

            [DllImport("libdl.so.2")]
            internal static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("libdl.so.2")]
            internal static extern IntPtr dlerror();

        }
    }
}
