using System;

namespace Microsoft.Diagnostics.Runtime.CorDebug
{
    static class PlatformHelper
    {
        public static bool IsWindows
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32Windows:
                    case PlatformID.Win32S:
                    case PlatformID.Win32NT:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}