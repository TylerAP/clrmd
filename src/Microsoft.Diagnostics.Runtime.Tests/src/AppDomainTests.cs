// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class AppDomainTests
    {
        static AppDomainTests() =>Helpers.InitHelpers();

        [Fact]
        public void ModuleDomainTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

#if !NETCOREAPP2_1
                ClrAppDomain appDomainExe = runtime.GetDomainByName("AppDomains.dll");
                Assert.NotNull(appDomainExe);
#else
                ClrAppDomain appDomainExe = runtime.GetDomainByName("clrhost");
                Assert.NotNull(appDomainExe);
#endif

#if !NETCOREAPP2_1
                ClrModule mscorlib = runtime.GetModule("mscorlib.dll");
#else
                ClrModule mscorlib = runtime.GetModule("System.Private.Corelib.dll");
#endif
                Assert.NotNull(mscorlib);

#if !NETCOREAPP2_1
                AssertModuleContainsDomains(mscorlib, runtime.SharedDomain, appDomainExe, nestedDomain);
#endif
                AssertModuleDoesntContainDomains(mscorlib, runtime.SystemDomain);

#if !NETCOREAPP2_1
                // SharedLibrary.dll is loaded into both domains but not as shared library like mscorlib.
                // This means it will not be in the shared domain.
                ClrModule sharedLibrary = runtime.GetModule("sharedlibrary.dll");
                Assert.NotNull(sharedLibrary);

                AssertModuleContainsDomains(sharedLibrary, appDomainExe, nestedDomain);
                AssertModuleDoesntContainDomains(sharedLibrary, runtime.SharedDomain, runtime.SystemDomain);
#endif

                ClrModule appDomainsExeModule = runtime.GetModule("AppDomains.dll");
                Assert.NotNull(appDomainsExeModule);

                AssertModuleContainsDomains(appDomainsExeModule, appDomainExe);

#if !NETCOREAPP2_1
                AssertModuleDoesntContainDomains(appDomainsExeModule, runtime.SystemDomain, runtime.SharedDomain, nestedDomain);

                ClrModule nestedExeModule = runtime.GetModule("NestedException.dll");
                Assert.NotNull(nestedExeModule);

                Assert.NotNull(nestedExeModule);

                AssertModuleContainsDomains(nestedExeModule, nestedDomain);
                AssertModuleDoesntContainDomains(nestedExeModule, runtime.SystemDomain, runtime.SharedDomain, appDomainExe);
#endif
            }
        }

        private void AssertModuleDoesntContainDomains(ClrModule module, params ClrAppDomain[] domainList)
        {
            IList<ClrAppDomain> moduleDomains = module.AppDomains;

            foreach (ClrAppDomain domain in domainList)
                Assert.False(moduleDomains.Contains(domain));
        }

        private void AssertModuleContainsDomains(ClrModule module, params ClrAppDomain[] domainList)
        {
            IList<ClrAppDomain> moduleDomains = module.AppDomains;

            foreach (ClrAppDomain domain in domainList)
                Assert.True(moduleDomains.Contains(domain));

            Assert.Equal(domainList.Length, moduleDomains.Count);
        }

        [Fact]
        public void AppDomainPropertyTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

                ClrAppDomain systemDomain = runtime.SystemDomain;
                Assert.Equal("System Domain", systemDomain.Name);
                Assert.NotEqual(0ul, systemDomain.Address);

                ClrAppDomain sharedDomain = runtime.SharedDomain;
                Assert.Equal("Shared Domain", sharedDomain.Name);
                Assert.NotEqual(0ul, sharedDomain.Address);

                Assert.NotEqual(systemDomain.Address, sharedDomain.Address);

#if !NETCOREAPP2_1
                Assert.Equal(2, runtime.AppDomains.Count);
                ClrAppDomain AppDomainsExe = runtime.AppDomains[0];
                Assert.Equal("AppDomains.dll", AppDomainsExe.Name);
                Assert.Equal(1, AppDomainsExe.Id);

                ClrAppDomain NestedExceptionExe = runtime.AppDomains[1];
                Assert.Equal("Second AppDomain", NestedExceptionExe.Name);
                Assert.Equal(2, NestedExceptionExe.Id);
#else
                Assert.Equal(1, runtime.AppDomains.Count);
                ClrAppDomain appDomainsExe = runtime.AppDomains.SingleOrDefault();
                Assert.NotNull(appDomainsExe);
                Assert.Equal(1, appDomainsExe.Id);
#endif
            }
        }

        [Fact]
        public void SystemAndSharedLibraryModulesTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

                ClrAppDomain systemDomain = runtime.SystemDomain;
                Assert.Equal(0, systemDomain.Modules.Count);

                ClrAppDomain sharedDomain = runtime.SharedDomain;
                Assert.Equal(1, sharedDomain.Modules.Count);

                ClrModule mscorlib = sharedDomain.Modules.Single();
#if !NETCOREAPP2_1
                Assert.Equal("mscorlib.dll", Path.GetFileName(mscorlib.FileName), true);
#else
                Assert.Equal("System.Private.Corelib.dll", Path.GetFileName(mscorlib.FileName), true);
#endif
            }
        }

        [Fact]
        public void ModuleAppDomainEqualityTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

#if !NETCOREAPP2_1
                ClrAppDomain appDomainsExe = runtime.GetDomainByName("AppDomains.exe");
                Assert.NotNull(appDomainsExe);
#else
                ClrAppDomain appDomainsExe = runtime.AppDomains.SingleOrDefault();
                Assert.NotNull(appDomainsExe);
#endif

                Dictionary<string, ClrModule> appDomainsModules = GetDomainModuleDictionary(appDomainsExe);

#if !NETCOREAPP2_1
                Assert.True(appDomainsModules.ContainsKey("appdomains.exe"));
                Assert.True(appDomainsModules.ContainsKey("mscorlib.dll"));
#else
                Assert.True(appDomainsModules.ContainsKey("appdomains.dll"));
                Assert.True(appDomainsModules.ContainsKey("System.Private.Corelib.dll"));
#endif

#if !NETCOREAPP2_1
                Assert.True(appDomainsModules.ContainsKey("sharedlibrary.dll"));

                Assert.False(appDomainsModules.ContainsKey("nestedexception.dll"));

                ClrAppDomain nestedExceptionExe = runtime.GetDomainByName("Second AppDomain");
                Assert.NotNull(nestedExceptionExe);
                Dictionary<string, ClrModule> nestedExceptionModules = GetDomainModuleDictionary(nestedExceptionExe);

                Assert.True(nestedExceptionModules.ContainsKey("nestedexception.dll"));
                Assert.True(nestedExceptionModules.ContainsKey("mscorlib.dll"));
                Assert.True(nestedExceptionModules.ContainsKey("sharedlibrary.dll"));

                Assert.False(nestedExceptionModules.ContainsKey("appdomains.dll"));

                // Ensure that we use the same ClrModule in each AppDomain.
                Assert.Equal(appDomainsModules["mscorlib.dll"], nestedExceptionModules["mscorlib.dll"]);
                Assert.Equal(appDomainsModules["sharedlibrary.dll"], nestedExceptionModules["sharedlibrary.dll"]);

#endif
            }
        }

        private static Dictionary<string, ClrModule> GetDomainModuleDictionary(ClrAppDomain domain)
        {
            Dictionary<string, ClrModule> result = new Dictionary<string, ClrModule>(StringComparer.OrdinalIgnoreCase);
            foreach (ClrModule module in domain.Modules)
                result.Add(Path.GetFileName(module.FileName), module);

            return result;
        }
    }
}