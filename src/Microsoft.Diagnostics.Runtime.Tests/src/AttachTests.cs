using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class AttachTests
    {
        static AttachTests() =>
            Helpers.InitHelpers();

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
                    Assert.NotNull(proc);

                    proc.WaitForExit(15);

                    proc.Refresh();

                    proc.HasExited.Should().BeFalse();

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

                        proc.HasExited.Should().BeTrue();
                    }
                }
            }
        }

        [Fact]
        public void PassiveAttachTest()
        {
            AttachBaseFunc(
                AttachFlag.Passive,
                dt =>
                    {
                        ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                        Assert.NotNull(runtime);

                        var appDom = runtime.AppDomains.SingleOrDefault();
                        Assert.NotNull(runtime);
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
                        Assert.NotNull(runtime);

                        var appDom = runtime.AppDomains.SingleOrDefault();
                        Assert.NotNull(runtime);
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
                        Assert.NotNull(runtime);

                        var appDom = runtime.AppDomains.SingleOrDefault();
                        Assert.NotNull(runtime);
                    });
        }
    }
}