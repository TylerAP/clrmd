// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class MethodTests
    {
        static MethodTests() =>Helpers.InitHelpers();

        [Fact]
        public void MethodHandleMultiDomainTests()
        {
            ulong[] methodDescs;
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

                // TODO: figure out why this isn't found, it should be
                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                Assert.NotNull(module);
                
                ClrType type = module.GetTypeByName("Foo");
                ClrMethod method = type.GetMethod("Bar");
                methodDescs = method.EnumerateMethodDescs().ToArray();

#if !NETCOREAPP2_1
                Assert.Equal(2, methodDescs.Length);
#else
                Assert.Single(methodDescs);
#endif
            }

            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);
                
                ClrMethod method = runtime.GetMethodByHandle(methodDescs[0]);

                Assert.NotNull(method);
                Assert.Equal("Bar", method.Name);
                Assert.Equal("Foo", method.Type.Name);
            }

#if !NETCOREAPP2_1
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);
                
                ClrMethod method = runtime.GetMethodByHandle(methodDescs[1]);

                Assert.NotNull(method);
                Assert.Equal("Bar", method.Name);
                Assert.Equal("Foo", method.Type.Name);
            }
#endif
        }

        [Fact]
        public void MethodHandleSingleDomainTests()
        {
            ulong methodDesc;
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                Assert.NotNull(module);
                
                ClrType type = module.GetTypeByName("Foo");
                ClrMethod method = type.GetMethod("Bar");
                methodDesc = method.EnumerateMethodDescs().Single();

                Assert.NotEqual(0ul, methodDesc);
            }

            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);
                
                ClrMethod method = runtime.GetMethodByHandle(methodDesc);

                Assert.NotNull(method);
                Assert.Equal("Bar", method.Name);
                Assert.Equal("Foo", method.Type.Name);
            }

            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                ClrType type = module.GetTypeByName("Foo");
                ClrMethod method = type.GetMethod("Bar");
                Assert.Equal(methodDesc, method.EnumerateMethodDescs().Single());
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
                Assert.NotNull(runtime);

                // TODO: figure out why this isn't found, it should be
                ClrModule module = runtime.GetModule("sharedlibrary.dll");
                Assert.NotNull(module);
                ClrType type = module.GetTypeByName("Foo");

                ClrMethod genericMethod = type.GetMethod("GenericBar");

                string methodName = genericMethod.GetFullSignature();

                Assert.Equal(')', methodName.Last());
            }
        }
	}
}