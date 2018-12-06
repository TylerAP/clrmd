// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class RuntimeTests
    {
        static RuntimeTests() =>Helpers.InitHelpers();

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

                badDac.ShouldNotBeNull();

                ClrInfo info = dt.ClrVersions.SingleOrDefault();

                info.ShouldNotBeNull();

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

                dac.ShouldNotBeNull();

                ClrRuntime runtime = info.CreateRuntime(dac);
                runtime.ShouldNotBeNull();
            }
        }

        [Fact]
        public void RuntimeClrInfo()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadFullDump())
            {
                ClrInfo info = dt.ClrVersions.SingleOrDefault();
                info.ShouldNotBeNull();

                ClrRuntime runtime = info.CreateRuntime();

                runtime.ClrInfo.ShouldBe(info);
            }
        }


        public static readonly IImmutableSet<string> ModuleEnumerationTestExpected
            = ImmutableHashSet.Create
            (
                StringComparer.OrdinalIgnoreCase,
#if !NETCOREAPP2_1
                "mscorlib.dll",
                "sharedlibrary.dll",
                "nestedexception.dll",
                "appdomains.dll"
#else
                "System.Private.CoreLib.dll",
                "System.Runtime.dll",
                "System.Runtime.Extensions.dll",
                "System.Threading.Thread.dll",
                "AppDomains.dll",
                "SharedLibrary.dll",
                "NestedException.dll"
#endif
            );
        
        [Fact]
        public void ModuleEnumerationTest()
        {
            // This test ensures that we enumerate all modules in the process exactly once.

            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                // TODO: where is NestedException.dll?
                IEnumerable<string> actual = runtime.Modules
                    .Select(m => Path.GetFileName(m.FileName));

                actual.ShouldBe(ModuleEnumerationTestExpected, true);
                //.BeEquivalentTo(expected);
            }
        }
    }
}