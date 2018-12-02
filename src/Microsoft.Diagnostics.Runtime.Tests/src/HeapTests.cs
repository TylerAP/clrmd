// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
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
                Assert.NotNull(runtime);
                ClrHeap heap = runtime.Heap;

                bool encounteredFoo = false;
                int count = 0;
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    ClrType type = heap.GetObjectType(obj);
                    Assert.NotNull(type);
                    if (type.Name == "Foo")
                        encounteredFoo = true;

                    count++;
                }

                encounteredFoo.Should().BeTrue();
                count.Should().BeGreaterThan(0);
            }
        }

        [Fact]
        public void HeapEnumerationMatches()
        {
            // Simply test that we can enumerate the heap.
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);
                ClrHeap heap = runtime.Heap;

                List<ClrObject> objects = new List<ClrObject>(heap.EnumerateObjects());

                int count = 0;
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    ClrObject actual = objects[count++];

                    actual.Address.Should().Be(obj);

                    ClrType type = heap.GetObjectType(obj);
                    
                    actual.Type.Should().BeSameAs(type);
                }

                count.Should().BeGreaterThan(0);
            }
        }

        [Fact]
        public void HeapCachedEnumerationMatches()
        {
            // Simply test that we can enumerate the heap.
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);
                ClrHeap heap = runtime.Heap;

                List<ClrObject> expectedList = new List<ClrObject>(heap.EnumerateObjects());

                heap.CacheHeap(CancellationToken.None);
                Assert.True(heap.IsHeapCached);
                List<ClrObject> actualList = new List<ClrObject>(heap.EnumerateObjects());

                Assert.True(actualList.Count > 0);
                Assert.Equal(expectedList.Count, actualList.Count);

                for (int i = 0; i < actualList.Count; i++)
                {
                    ClrObject expected = expectedList[i];
                    ClrObject actual = actualList[i];

                    actual.Should().Be(expected);
                }
            }
        }

        [Fact]
        public void ServerSegmentTests()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump(GCMode.Server))
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);
                ClrHeap heap = runtime.Heap;

                runtime.ServerGC.Should().BeTrue();

                CheckSegments(heap);
            }
        }

        [Fact]
        public void WorkstationSegmentTests()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);
                ClrHeap heap = runtime.Heap;

                runtime.ServerGC.Should().BeFalse();

                CheckSegments(heap);
            }
        }

        private static void CheckSegments(ClrHeap heap)
        {
            foreach (ClrSegment seg in heap.Segments)
            {
                seg.Start.Should().NotBe(0ul);
                seg.End.Should().NotBe(0ul);
                seg.Start.Should().BeLessOrEqualTo(seg.End);

                seg.Start.Should().BeLessThan(seg.CommittedEnd);
                seg.CommittedEnd.Should().BeLessThan(seg.ReservedEnd);

                if (!seg.IsEphemeral)
                {
                    seg.Gen0Length.Should().Be(0ul);
                    seg.Gen1Length.Should().Be(0ul);
                }
                
                foreach (ulong obj in seg.EnumerateObjectAddresses())
                {
                    ClrSegment curr = heap.GetSegmentByAddress(obj);
                    curr.Should().BeSameAs(seg);
                }
            }
        }
    }
}