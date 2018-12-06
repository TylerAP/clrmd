using System;
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
            using (var proc = Process.Start(new ProcessStartInfo(s_dotnetPath, s_asmPath)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                try
                {
                    proc.ShouldNotBeNull();

                    proc.WaitForExit(45);

                    proc.Refresh();

                    proc.HasExited.ShouldBeFalse();

                    var pid = proc.Id;
                    using (DataTarget dt = DataTarget.AttachToProcess(pid, 1000, attachFlag))
                    {
                        action(dt);
                    }
                }
                finally
                {
                    if (proc != null)
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
        }

        [Fact]
        public void PassiveAttachTest()
        {
            Console.SetError( new TestOutputHelper(_output) );
            
            AttachBaseFunc(
                AttachFlag.Passive,
                dt =>
                    {
                        dt.ShouldBeOfType<XPlatLiveDataTarget>();

                        Console.Error.WriteLine("Got XPlatLiveDataTarget");
                        
                        dt.EnumerateModules().ShouldNotBeEmpty();

                        Console.Error.WriteLine("Got Modules");

                        dt.ClrVersions.ShouldNotBeEmpty();
                        dt.ClrVersions.ShouldNotContain((ClrInfo)null);
                        
                        Console.Error.WriteLine("Got ClrVersions");

                        ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                        runtime.ShouldNotBeNull();

                        Console.Error.WriteLine("Got Runtime");

                        var appDom = runtime.AppDomains.SingleOrDefault();
                        runtime.ShouldNotBeNull();

                        Console.Error.WriteLine("Got AppDomains");

                    });
        }

        [Fact]
        public void NonInvasiveAttachTest()
        {
            AttachBaseFunc(
                AttachFlag.NonInvasive,
                dt =>
                    {
                        ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                        runtime.ShouldNotBeNull();

                        var appDom = runtime.AppDomains.SingleOrDefault();
                        runtime.ShouldNotBeNull();
                    });
        }

        [Fact]
        public void InvasiveAttachTest()
        {
            AttachBaseFunc(
                AttachFlag.Invasive,
                dt =>
                    {
                        ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                        runtime.ShouldNotBeNull();

                        var appDom = runtime.AppDomains.SingleOrDefault();
                        runtime.ShouldNotBeNull();
                    });
        }
    }
}