// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class GCHandleTests
    {
        static GCHandleTests() =>Helpers.InitHelpers();

        [Fact]
        public void EnsureEnumerationStability()
        {
            // I made some changes to v4.5 handle enumeration to enumerate handles out faster.
            // This test makes sure I have a stable enumeration.
            using (DataTarget dt = TestTargets.GCHandles.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();

                List<ClrHandle> handles = new List<ClrHandle>(runtime.EnumerateHandles());

                int i = 0;
                foreach (ClrHandle hnd in runtime.EnumerateHandles())
                    ( hnd).ShouldBe(handles[i++]);

                // We create at least this many handles in the test, plus the runtime uses some.
                (handles.Count > 4).ShouldBeTrue();
            }
        }

        [Fact]
        public void EnsureAllItemsAreUnique()
        {
            // Making sure that handles are returned only once
            HashSet<ClrHandle> handles = new HashSet<ClrHandle>();

            using (DataTarget dt = TestTargets.GCHandles.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();

                foreach (ClrHandle handle in runtime.EnumerateHandles())
                    (handles.Add(handle)).ShouldBeTrue();

                // Make sure we had at least one AsyncPinned handle
                Assert.Contains(handles, h => h.HandleType == HandleType.AsyncPinned);
            }
        }
    }
}