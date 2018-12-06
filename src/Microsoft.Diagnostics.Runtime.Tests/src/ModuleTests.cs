// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class ModuleTests
    {
        static ModuleTests() =>Helpers.InitHelpers();

        [Fact]
        public void TestGetTypeByName()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                
                ClrHeap heap = runtime.Heap;
                heap.ShouldNotBeNull();

                ClrModule shared = runtime.GetModule("sharedlibrary.dll");
                shared.ShouldNotBeNull();
                shared.GetTypeByName("Foo").ShouldNotBeNull();
                Assert.Null(shared.GetTypeByName("Types"));

                ClrModule types = runtime.GetModule("types.dll");
                types.GetTypeByName("Types").ShouldNotBeNull();
                Assert.Null(types.GetTypeByName("Foo"));
            }
        }
    }
}