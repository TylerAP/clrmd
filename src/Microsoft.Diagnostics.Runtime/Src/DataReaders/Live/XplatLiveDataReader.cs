// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.Runtime.Utilities;
using static Microsoft.Diagnostics.Runtime.Utilities.WindowsNativeMethods;

namespace Microsoft.Diagnostics.Runtime
{
    internal unsafe class XplatLiveDataReader : IDataReader
    {
        private readonly int _pid;
        private static readonly Regex s_rxProcMaps = new Regex(
            @"^([0-9a-fA-F]+)-([0-9a-fA-F]+) ([a-zA-Z0-9_\-]{4,}) ([0-9a-fA-F]+) ([0-9a-fA-F]{2,}:[0-9a-fA-F]{2,}) (\d+)(?:[ \t]+([^\s].*?))?\s*$",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

        public XplatLiveDataReader(int pid)
        {
            _pid = pid;

            // TODO: check dac arch
        }

        public bool IsMinidump => false;

        public void Close()
        {
        }

        public void Flush()
        {
        }

        public Architecture GetArchitecture()
        {
            if (IntPtr.Size == 4)
                return Architecture.X86;

            return Architecture.Amd64;
        }

        public uint GetPointerSize()
        {
            return (uint)IntPtr.Size;
        }

        public IList<ModuleInfo> EnumerateModules()
        {
            using (var proc = Process.GetProcessById(_pid))
            {
                return proc.Modules
                    .Cast<ProcessModule>()
                    .Select(
                        module => new ModuleInfo(this)
                        {
                            ImageBase = unchecked((ulong)module.BaseAddress.ToInt64()),
                            FileName = module.FileName,
                            FileSize = unchecked((uint)module.ModuleMemorySize),
                            TimeStamp = 0 // really?
                        })
                    .ToArray();
            }
        }

        public void GetVersionInfo(ulong addr, out VersionInfo version)
        {
            string filename;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StringBuilder sbFilename = new StringBuilder(1024);
                using (Process p = Process.GetProcessById(_pid))
                    GetModuleFileNameExA(p.Handle, new IntPtr((long)addr), sbFilename, sbFilename.Capacity);
                filename = sbFilename.ToString();

            }
            else
            {
                filename = GetModuleFileNameXplat(addr);
            }
            
            if (DataTarget.PlatformFunctions.GetFileVersion(filename, out int major, out int minor, out int revision, out int patch))
                version = new VersionInfo(major, minor, revision, patch);
            else
                version = new VersionInfo();
            
        }

        public bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
        {
            fixed (byte* pByte = &buffer[0])
            {
                return ReadMemory(address, (IntPtr)pByte, bytesRequested, out bytesRead);
            }
        }

        public bool ReadMemory(ulong address, IntPtr buffer, int bytesRequested, out int bytesRead)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using (var p = Process.GetProcessById(_pid))
                    {
                        return ReadProcessMemory(p.Handle, new IntPtr((long)address), buffer, bytesRequested, out bytesRead);
                    }
                }
                catch
                {
                    bytesRead = 0;
                    return false;
                }
            }

            ulong requested = unchecked((ulong)bytesRequested);
            ulong offset = 0;
            do
            {
                var len = (UIntPtr)Math.Min(requested - offset, 1024);

                var local = new iovec
                {
                    iov_base = (IntPtr)(unchecked((ulong)buffer.ToInt64()) + offset),
                    iov_len = len
                };

                var remote = new iovec
                {
                    iov_base = (IntPtr)(address + offset),
                    iov_len = len
                };

                var read = LinuxFunctions.process_vm_readv(_pid, local, 1, remote, 1);

                offset += read.ToUInt64();

                if (read == len)
                    continue;

                // incomplete read, assume error? do we return false?
                bytesRead = (int)offset;
                return false;
            } while (offset < requested);

            bytesRead = (int)offset;

            return true;
        }

        public TValue Read<TValue>(ulong addr)
        {
            var buf = new byte[Unsafe.SizeOf<TValue>()];

            if (!ReadMemory(addr, buf, buf.Length, out int bytesRead))
                throw new NotImplementedException("Incomplete read");
            if (bytesRead != buf.Length)
                throw new NotImplementedException("Incomplete read");

            return Unsafe.ReadUnaligned<TValue>(ref buf[0]);
        }

        public ulong ReadPointerUnsafe(ulong addr)
        {
            return Read<UIntPtr>(addr).ToUInt64();
        }

        public uint ReadDwordUnsafe(ulong addr)
        {
            return Read<uint>(addr);
        }

        public ulong GetThreadTeb(uint thread)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<uint> EnumerateAllThreads()
        {
            using (Process p = Process.GetProcessById(_pid))
                foreach (ProcessThread thread in p.Threads)
                    yield return (uint)thread.Id;
        }

        public bool VirtualQuery(ulong addr, out VirtualQueryData vq)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                vq = new VirtualQueryData();

                MEMORY_BASIC_INFORMATION mem = new MEMORY_BASIC_INFORMATION();
                IntPtr ptr = new IntPtr((long)addr);

                using (Process p = Process.GetProcessById(_pid))
                    if (VirtualQueryEx(p.Handle, ptr, ref mem, new IntPtr(Marshal.SizeOf(mem))) == 0)
                        return false;

                vq.BaseAddress = mem.BaseAddress;
                vq.Size = mem.Size;
                return true;
            }
            else
            {
                using (var sr = new StreamReader($"/proc/{_pid}/maps", Encoding.UTF8, false, 81908))
                {
                    do
                    {
                        var line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            break;

                        var match = s_rxProcMaps.Match(line);
                        if (!match.Success)
                            throw new NotImplementedException("don't understand /proc/pid/map");

                        var start = ulong.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
                        var end = ulong.Parse(match.Groups[2].Value, NumberStyles.HexNumber);
                        var perms = match.Groups[3].Value;
                        var offset = match.Groups[4].Value;
                        var device = match.Groups[5].Value;
                        var inode = match.Groups[6].Value;
                        var pathname = match.Groups.Count == 8 ? match.Groups[7].Value : "";

                        if (addr >= start && addr <= end)
                        {
                            vq = new VirtualQueryData(start, end - start);
                            return true;
                        }
                    } while (!sr.EndOfStream);
                }

                vq = default;
                return false;
            }
        }

        private string GetModuleFileNameXplat(ulong addr)
        {
            using (var sr = new StreamReader($"/proc/{_pid}/maps", Encoding.UTF8, false, 81908))
            {
                do
                {
                    var line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        break;

                    var match = s_rxProcMaps.Match(line);
                    if (!match.Success)
                        throw new NotImplementedException("don't understand /proc/pid/map");

                    var start = ulong.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
                    var end = ulong.Parse(match.Groups[2].Value, NumberStyles.HexNumber);
                    var perms = match.Groups[3].Value;
                    var offset = match.Groups[4].Value;
                    var device = match.Groups[5].Value;
                    var inode = match.Groups[6].Value;
                    var pathname = match.Groups.Count == 8 ? match.Groups[7].Value : "";

                    if (addr >= start && addr <= end)
                    {
                        return pathname;
                    }
                } while (!sr.EndOfStream);
            }
            return null;
        }

        public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, IntPtr context)
        {
            throw new NotImplementedException();
        }

        public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte[] context)
        {
            throw new NotImplementedException();
        }
    }
}