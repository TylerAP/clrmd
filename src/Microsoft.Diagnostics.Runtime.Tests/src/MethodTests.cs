// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class MethodTests
    {
        static MethodTests() =>
            Helpers.InitHelpers();

        [Fact]
        public void MethodHandleMultiDomainTests()
        {
            ulong[] methodDescs;
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                // TODO: figure out why this isn't found, it should be
                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                module.ShouldNotBeNull();

                ClrType type = module.GetTypeByName("Foo");
                ClrMethod method = type.GetMethod("Bar");
                methodDescs = method.EnumerateMethodDescs().ToArray();

#if !NETCOREAPP2_1
                ( methodDescs.Length).ShouldBe(2);
#else
                Assert.Single(methodDescs);
#endif
            }

            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrMethod method = runtime.GetMethodByHandle(methodDescs[0]);

                method.ShouldNotBeNull();
                method.Name.ShouldBe("Bar");
                method.Type.Name.ShouldBe("Foo");
            }

#if !NETCOREAPP2_1
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                
                ClrMethod method = runtime.GetMethodByHandle(methodDescs[1]);

                (method).ShouldNotBeNull();
                ( method.Name).ShouldBe("Bar");
                ( method.Type.Name).ShouldBe("Foo");
            }
#endif
        }

        [Fact]
        public void MethodHandleSingleDomainTest1()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                module.ShouldNotBeNull();

                ClrType type = module.GetTypeByName("Foo");
                ClrMethod method = type.GetMethod("Bar");
                ulong methodDesc = method.EnumerateMethodDescs().Single();

                methodDesc.ShouldNotBe(0ul);
            }
        }

        [Fact]
        public void MethodHandleSingleDomainTest2()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                module.ShouldNotBeNull();
                ClrType type = module.GetTypeByName("Foo");
                ClrMethod barMethod = type.GetMethod("Bar");
                ulong methodDesc = barMethod.EnumerateMethodDescs().Single();
                ClrMethod method = runtime.GetMethodByHandle(methodDesc);

                method.ShouldNotBeNull();
                method.Name.ShouldBe("Bar");
                method.Type.Name.ShouldBe("Foo");
            }
        }

        [Fact]
        public void MethodHandleSingleDomainTest3()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                module.ShouldNotBeNull();
                ClrType type = module.GetTypeByName("Foo");
                ClrMethod method = type.GetMethod("Bar");
                method.EnumerateMethodDescs().ShouldHaveSingleItem();
            }
        }

        /// <summary>
        /// This test tests a patch in v45runtime.GetNameForMD(ulong md) that
        /// corrects an error from sos
        /// </summary>
        [Fact]
        public void CompleteSignatureIsRetrievedForMethodsWithGenericParameters()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                // TODO: figure out why this isn't found, it should be
                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                module.ShouldNotBeNull();
                ClrType type = module.GetTypeByName("Foo");

                ClrMethod genericMethod = type.GetMethod("GenericBar");

                string methodName = genericMethod.GetFullSignature();

                methodName.Last().ShouldBe(')');
            }
        }
    }
}