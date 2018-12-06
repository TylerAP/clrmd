// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class HeapTests
    {
        static HeapTests() =>Helpers.InitHelpers();

        [Fact]
        public void HeapEnumeration()
        {
            // Simply test that we can enumerate the heap.
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                bool encounteredFoo = false;
                int count = 0;
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    ClrType type = heap.GetObjectType(obj);
                    type.ShouldNotBeNull();
                    if (type.Name == "Foo")
                        encounteredFoo = true;

                    count++;
                }

                encounteredFoo.ShouldBeTrue();
                count.ShouldBeGreaterThan(0);
            }
        }

        [Fact]
        public void HeapEnumerationMatches()
        {
            // Simply test that we can enumerate the heap.
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                List<ClrObject> objects = new List<ClrObject>(heap.EnumerateObjects());

                int count = 0;
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    ClrObject actual = objects[count++];

                    actual.Address.ShouldBe(obj);

                    ClrType type = heap.GetObjectType(obj);
                    
                    actual.Type.ShouldBeSameAs(type);
                }

                count.ShouldBeGreaterThan(0);
            }
        }

        [Fact]
        public void HeapCachedEnumerationMatches()
        {
            // Simply test that we can enumerate the heap.
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                List<ClrObject> expectedList = new List<ClrObject>(heap.EnumerateObjects());

                heap.CacheHeap(CancellationToken.None);
                heap.IsHeapCached.ShouldBeTrue();
                List<ClrObject> actualList = new List<ClrObject>(heap.EnumerateObjects());

                (actualList.Count > 0).ShouldBeTrue();
                actualList.Count.ShouldBe(expectedList.Count);

                for (int i = 0; i < actualList.Count; i++)
                {
                    ClrObject expected = expectedList[i];
                    ClrObject actual = actualList[i];

                    actual.ShouldBe(expected);
                }
            }
        }

        [Fact]
        public void ServerSegmentTests()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump(GCMode.Server))
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                runtime.ServerGC.ShouldBeTrue();

                CheckSegments(heap);
            }
        }

        [Fact]
        public void WorkstationSegmentTests()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                runtime.ServerGC.ShouldBeFalse();

                CheckSegments(heap);
            }
        }

        private static void CheckSegments(ClrHeap heap)
        {
            foreach (ClrSegment seg in heap.Segments)
            {
                seg.Start.ShouldNotBe(0ul);
                seg.End.ShouldNotBe(0ul);
                seg.Start.ShouldBeLessThanOrEqualTo(seg.End);

                seg.Start.ShouldBeLessThan(seg.CommittedEnd);
                seg.CommittedEnd.ShouldBeLessThan(seg.ReservedEnd);

                if (!seg.IsEphemeral)
                {
                    seg.Gen0Length.ShouldBe(0ul);
                    seg.Gen1Length.ShouldBe(0ul);
                }
                
                foreach (ulong obj in seg.EnumerateObjectAddresses())
                {
                    ClrSegment curr = heap.GetSegmentByAddress(obj);
                    curr.ShouldBeSameAs(seg);
                }
            }
        }
    }
}