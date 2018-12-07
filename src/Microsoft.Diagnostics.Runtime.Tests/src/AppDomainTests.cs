// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shouldly;
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
                runtime.ShouldNotBeNull();

#if !NETCOREAPP2_1
                ClrAppDomain appDomainExe = runtime.GetDomainByName("AppDomains.dll");
                (appDomainExe).ShouldNotBeNull();
#else
                ClrAppDomain appDomainExe = runtime.GetDomainByName("clrhost");
                appDomainExe.ShouldNotBeNull();
#endif

#if !NETCOREAPP2_1
                ClrModule mscorlib = runtime.GetModule("mscorlib.dll");
#else
                ClrModule mscorlib = runtime.GetModule("System.Private.Corelib.dll");
#endif
                mscorlib.ShouldNotBeNull();

#if !NETCOREAPP2_1
                AssertModuleContainsDomains(mscorlib, runtime.SharedDomain, appDomainExe, nestedDomain);
#endif
                AssertModuleDoesntContainDomains(mscorlib, runtime.SystemDomain);

#if !NETCOREAPP2_1
                // SharedLibrary.dll is loaded into both domains but not as shared library like mscorlib.
                // This means it will not be in the shared domain.
                ClrModule sharedLibrary = runtime.GetModule("sharedlibrary.dll");
                (sharedLibrary).ShouldNotBeNull();

                AssertModuleContainsDomains(sharedLibrary, appDomainExe, nestedDomain);
                AssertModuleDoesntContainDomains(sharedLibrary, runtime.SharedDomain, runtime.SystemDomain);
#endif

                ClrModule appDomainsExeModule = runtime.GetModule("AppDomains.dll");
                appDomainsExeModule.ShouldNotBeNull();

                AssertModuleContainsDomains(appDomainsExeModule, appDomainExe);

#if !NETCOREAPP2_1
                AssertModuleDoesntContainDomains(appDomainsExeModule, runtime.SystemDomain, runtime.SharedDomain, nestedDomain);

                ClrModule nestedExeModule = runtime.GetModule("NestedException.dll");
                (nestedExeModule).ShouldNotBeNull();

                (nestedExeModule).ShouldNotBeNull();

                AssertModuleContainsDomains(nestedExeModule, nestedDomain);
                AssertModuleDoesntContainDomains(nestedExeModule, runtime.SystemDomain, runtime.SharedDomain, appDomainExe);
#endif
            }
        }

        private void AssertModuleDoesntContainDomains(ClrModule module, params ClrAppDomain[] domainList)
        {
            IList<ClrAppDomain> moduleDomains = module.AppDomains;

            foreach (ClrAppDomain domain in domainList)
                moduleDomains.Contains(domain).ShouldBeFalse();
        }

        private void AssertModuleContainsDomains(ClrModule module, params ClrAppDomain[] domainList)
        {
            IList<ClrAppDomain> moduleDomains = module.AppDomains;

            foreach (ClrAppDomain domain in domainList)
                moduleDomains.Contains(domain).ShouldBeTrue();

            moduleDomains.Count.ShouldBe(domainList.Length);
        }

        [Fact]
        public void AppDomainPropertyTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrAppDomain systemDomain = runtime.SystemDomain;
                systemDomain.Name.ShouldBe("System Domain");
                systemDomain.Address.ShouldNotBe(0ul);

                ClrAppDomain sharedDomain = runtime.SharedDomain;
                sharedDomain.Name.ShouldBe("Shared Domain");
                sharedDomain.Address.ShouldNotBe(0ul);

                sharedDomain.Address.ShouldNotBe(systemDomain.Address);

#if !NETCOREAPP2_1
                ( runtime.AppDomains.Count).ShouldBe(2);
                ClrAppDomain AppDomainsExe = runtime.AppDomains[0];
                ( AppDomainsExe.Name).ShouldBe("AppDomains.dll");
                ( AppDomainsExe.Id).ShouldBe(1);

                ClrAppDomain NestedExceptionExe = runtime.AppDomains[1];
                ( NestedExceptionExe.Name).ShouldBe("Second AppDomain");
                ( NestedExceptionExe.Id).ShouldBe(2);
#else
                runtime.AppDomains.Count.ShouldBe(1);
                ClrAppDomain appDomainsExe = runtime.AppDomains.SingleOrDefault();
                appDomainsExe.ShouldNotBeNull();
                appDomainsExe.Id.ShouldBe(1);
#endif
            }
        }

        [Fact]
        public void SystemAndSharedLibraryModulesTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrAppDomain systemDomain = runtime.SystemDomain;
                systemDomain.Modules.Count.ShouldBe(0);

                ClrAppDomain sharedDomain = runtime.SharedDomain;
                sharedDomain.Modules.Count.ShouldBe(1);

                ClrModule mscorlib = sharedDomain.Modules.Single();
#if !NETCOREAPP2_1
                Path.GetFileName(mscorlib.FileName).ShouldBe("mscorlib.dll", Case.Insensitive);
#else
                Path.GetFileName(mscorlib.FileName).ShouldBe("System.Private.Corelib.dll", StringCompareShould.IgnoreCase);
#endif
            }
        }

        [Fact]
        public void ModuleAppDomainEqualityTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

#if !NETCOREAPP2_1
                ClrAppDomain appDomainsExe = runtime.GetDomainByName("AppDomains.exe");
                (appDomainsExe).ShouldNotBeNull();
#else
                ClrAppDomain appDomainsExe = runtime.AppDomains.SingleOrDefault();
                appDomainsExe.ShouldNotBeNull();
#endif

                Dictionary<string, ClrModule> appDomainsModules = GetDomainModuleDictionary(appDomainsExe);

#if !NETCOREAPP2_1
                (appDomainsModules).ShouldContainKey("appdomains.exe");
                (appDomainsModules).ShouldContainKey("mscorlib.dll");
#else
                appDomainsModules.ShouldContainKey("appdomains.dll");
                appDomainsModules.ShouldContainKey("System.Private.Corelib.dll");
#endif

#if !NETCOREAPP2_1
                (appDomainsModules).ShouldContainKey("sharedlibrary.dll");

                (appDomainsModules.ContainsKey("nestedexception.dll")).ShouldBeFalse();

                ClrAppDomain nestedExceptionExe = runtime.GetDomainByName("Second AppDomain");
                (nestedExceptionExe).ShouldNotBeNull();
                Dictionary<string, ClrModule> nestedExceptionModules = GetDomainModuleDictionary(nestedExceptionExe);

                (nestedExceptionModules).ShouldContainKey("nestedexception.dll");
                (nestedExceptionModules).ShouldContainKey("mscorlib.dll");
                (nestedExceptionModules).ShouldContainKey("sharedlibrary.dll");

                (nestedExceptionModules.ContainsKey("appdomains.dll")).ShouldBeFalse();

                // Ensure that we use the same ClrModule in each AppDomain.
                ( nestedExceptionModules["mscorlib.dll"]).ShouldBe(appDomainsModules["mscorlib.dll"]);
                ( nestedExceptionModules["sharedlibrary.dll"]).ShouldBe(appDomainsModules["sharedlibrary.dll"]);

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