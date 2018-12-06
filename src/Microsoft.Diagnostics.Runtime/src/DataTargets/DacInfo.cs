// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

#pragma warning disable 0618

namespace Microsoft.Diagnostics.Runtime
{
    /// <summary>
    /// Represents the dac dll
    /// </summary>
    [Serializable]
    public class DacInfo : ModuleInfo
    {
        /// <summary>
        /// Returns the filename of the dac dll according to the specified parameters
        /// </summary>
        public static string GetDacRequestFileName(ClrFlavor flavor, Architecture currentArchitecture, Architecture targetArchitecture, VersionInfo clrVersion)
        {
            string dacName = flavor == ClrFlavor.Core
                ? "mscordaccore"
                : "mscordacwks";
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"{dacName}_{currentArchitecture}_{targetArchitecture}_{clrVersion.Major}.{clrVersion.Minor}.{clrVersion.Revision}.{clrVersion.Patch:D2}.dll"
                : $"lib{dacName}.so";
        }

        internal static string GetDacFileName(ClrFlavor flavor, Runtime.Architecture targetArchitecture) {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? flavor == ClrFlavor.Core
                    ? "mscordaccore.dll"
                    : "mscordacwks.dll"
                : flavor == ClrFlavor.Core
                    ? "libmscordaccore.so"
                    : "libmscordacwks.so";
        }

        /// <summary>
        /// The platform-agnostice filename of the dac dll
        /// </summary>
        public string PlatformAgnosticFileName { get; set; }

        /// <summary>
        /// The architecture (x86 or amd64) being targeted
        /// </summary>
        public Architecture TargetArchitecture { get; set; }

        /// <summary>
        /// Constructs a DacInfo object with the appropriate properties initialized
        /// </summary>
        public DacInfo(IDataReader reader, string agnosticName, Architecture targetArch)
            : base(reader)
        {
            PlatformAgnosticFileName = agnosticName;
            TargetArchitecture = targetArch;
        }
    }
}