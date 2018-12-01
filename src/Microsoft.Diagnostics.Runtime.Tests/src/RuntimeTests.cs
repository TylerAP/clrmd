// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class RuntimeTests
    {
        [Fact]
        public void CreationSpecificDacNegativeTest()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadFullDump())
            {
                string badDac = dt.SymbolLocator.FindBinary(
                    SymbolLocatorTests.WellKnownDac,
                    SymbolLocatorTests.WellKnownDacTimeStamp,
                    SymbolLocatorTests.WellKnownDacImageSize,
                    false);

                Assert.NotNull(badDac);

                ClrInfo info = dt.ClrVersions.SingleOrDefault();

                Assert.NotNull(info);

                Assert.Throws<InvalidOperationException>(() => info.CreateRuntime(badDac));
            }
        }

        [Fact]
        public void CreationSpecificDac()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadFullDump())
            {
                ClrInfo info = dt.ClrVersions.Single();
                string dac = info.LocalMatchingDac;

                Assert.NotNull(dac);

                ClrRuntime runtime = info.CreateRuntime(dac);
                Assert.NotNull(runtime);
            }
        }

        [Fact]
        public void RuntimeClrInfo()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadFullDump())
            {
                ClrInfo info = dt.ClrVersions.SingleOrDefault();
                Assert.NotNull(info);

                ClrRuntime runtime = info.CreateRuntime();

                Assert.Equal(info, runtime.ClrInfo);
            }
        }

        [Fact]
        public void ModuleEnumerationTest()
        {
            // This test ensures that we enumerate all modules in the process exactly once.

            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

                HashSet<string> expected = new HashSet<string>(
#if !NETCOREAPP2_1
                    new[] {"mscorlib.dll", "sharedlibrary.dll", "nestedexception.dll", "appdomains.dll"},
#else
                    new[] {
                        "System.Private.Corelib.dll", "System.Runtime.dll", "System.Runtime.Extensions.dll", "System.ThreadingThread.dll",
                        "AppDomains.dll", "SharedLibrary.dll", "NestedException.dll"
                    },
#endif
                    StringComparer.OrdinalIgnoreCase);

                // TODO: where is SharedLibrary.dll and NestedException.dll?
                expected.Should().Equal(runtime.Modules.Select(m => Path.GetFileName(m.FileName)));
            }
        }
    }
}