using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class AttachTests
    {
        private readonly ITestOutputHelper _output;

        static AttachTests() =>
            Helpers.InitHelpers();

        public AttachTests(ITestOutputHelper output) =>
            _output = output;

        private static readonly string s_dotnetPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Environment.ExpandEnvironmentVariables("%ProgramFiles%\\dotnet\\dotnet.exe")
            : "dotnet";

        private static readonly string s_asmPath
            = new Uri(
                System.IO.Path.Combine(
                    Environment.CurrentDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "TestTargets",
                    "bin",
                    $"x{(IntPtr.Size == 4 ? 86 : 64)}",
                    "Attach.dll")).LocalPath;

        private void AttachBaseFunc(AttachFlag attachFlag, Action<DataTarget> action)
        {
            var proc = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo(s_dotnetPath, s_asmPath)
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            proc.Start().ShouldBeTrue();

            using (proc)
            {
                try
                {
                    proc.ShouldNotBeNull();

                    proc.WaitForExit(45);

                    proc.Refresh();

                    proc.HasExited.ShouldBeFalse();

                    string dataRecv = proc.StandardOutput.ReadLine();

                    dataRecv.ShouldBe("Hello");

                    int pid = proc.Id;

                    using (DataTarget dt = DataTarget.AttachToProcess(pid, 1000, attachFlag))
                        action(dt);
                }
                finally
                {
                    proc.StandardInput.Write(0x03);
                    proc.WaitForExit(15);
                    proc.StandardInput.Close();
                    if (!proc.HasExited)
                    {
                        proc.Kill();
                        proc.WaitForExit(15);
                    }

                    proc.HasExited.ShouldBeTrue();
                }
            }
        }

        [Fact]
        public void PassiveAttachTest()
        {
            Console.SetError(new TestOutputHelper(_output));

            AttachBaseFunc(
                AttachFlag.Passive,
                dt =>
                    {
                        GenericAttachProcAssertions(dt);
                    });
        }

        [Fact]
        public void NonInvasiveAttachTest()
        {
            AttachBaseFunc(
                AttachFlag.NonInvasive,
                dt =>
                    {
                        GenericAttachProcAssertions(dt);
                    });
        }

        [Fact]
        public void InvasiveAttachTest()
        {
            AttachBaseFunc(
                AttachFlag.Invasive,
                dt =>
                    {
                        GenericAttachProcAssertions(dt);
                    });
        }

        private void GenericAttachProcAssertions(DataTarget dt)
        {
            Console.Error.WriteLine("Got XPlatLiveDataTarget");

            dt.EnumerateModules().ShouldNotBeEmpty();

            Console.Error.WriteLine("Modules:");
            Console.Error.WriteLine("           Address        Size Name");
            foreach (var m in dt.EnumerateModules())
            {
                Console.Error.WriteLine($"0x{m.ImageBase:x16} {m.FileSize,11} {m.FileName}");
            }

            dt.ClrVersions.ShouldNotBeEmpty();
            dt.ClrVersions.ShouldNotContain((ClrInfo)null);

            Console.Error.WriteLine("Got ClrVersions");

            Console.Error.WriteLine("CLR Versions:");
            Console.Error.WriteLine("             Version      Flavor DAC");
            foreach (var v in dt.ClrVersions)
            {
                Console.Error.WriteLine($"0x{v.Version.ToString(),20} {v.Flavor,11} {v.LocalMatchingDac}");
            }

            ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
            runtime.ShouldNotBeNull();

            Console.Error.WriteLine($"Runtime GC: {(runtime.ServerGC ? "SVR" : "WKS")}");

            var appDom = runtime.AppDomains.SingleOrDefault();

            runtime.ShouldNotBeNull();

            appDom.ShouldNotBeNull();

            Console.Error.WriteLine("Got AppDomains");

            appDom.Modules.ShouldNotBeEmpty();

            Console.Error.WriteLine("Memory Regions:");
            Console.Error.WriteLine("           Address        Size            AppDomain Type");
            foreach (var r in runtime.EnumerateMemoryRegions())
            {
                Console.Error.WriteLine($"0x{r.Address:x16} {r.Size,11} {r.AppDomain,20} {r.Type}");
            }

            Console.Error.WriteLine("Handles:");
            Console.Error.WriteLine("           Address            Handle  HandleType TypeName");
            foreach (ClrHandle h in runtime.EnumerateHandles())
            {
                Console.Error.WriteLine($"0x{h.Address:x16} 0x{h.Object:x16} {h.HandleType,11} {h.Type?.Name}");
            }

            Console.Error.WriteLine("Threads:");
            Console.Error.WriteLine("           Address  StackDepth MethodName");
            foreach (ClrThread t in runtime.Threads)
            {
                IList<ClrStackFrame> st = t.StackTrace;
                ClrStackFrame f = st?.FirstOrDefault();
                Console.Error.WriteLine($"0x{t.Address:x16} {st?.Count,11} {f?.Method?.Type?.Name}.{f?.Method?.Name}");
            }
            
            Assert.False(true);
        }
    }
}