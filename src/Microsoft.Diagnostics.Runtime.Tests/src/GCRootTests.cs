// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class GCRootTests
    {
        static GCRootTests() =>Helpers.InitHelpers();

        [Fact]
        public void EnumerateGCRefs()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;

                ulong obj = heap.GetObjectsOfType("DoubleRef").Single();
                ClrType type = heap.GetObjectType(obj);

                ClrObject[] refs = type.EnumerateObjectReferences(obj).ToArray();
                ValidateRefs(refs);
            }
        }

        private void ValidateRefs(ClrObject[] refs)
        {
            // Should contain one SingleRef and one TripleRef object.
            refs.Length.ShouldBe(2);

            refs.Count(r => r.Type.Name == "SingleRef").ShouldBe(1);
            refs.Count(r => r.Type.Name == "TripleRef").ShouldBe(1);

            foreach (ClrObject obj in refs)
            {
                obj.Address.ShouldNotBe(0ul);
                obj.Type.ShouldBe(obj.Type.Heap.GetObjectType(obj.Address));
            }
        }

        [Fact]
        public void EnumerateGCRefsArray()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;

                ClrModule module = heap.Runtime.GetMainModule();
                ClrType mainType = module.GetTypeByName("GCRootTarget");

                ClrObject obj = mainType.GetStaticObjectValue("TheRoot");
                obj = obj.GetObjectField("Item1");

                obj.Type.Name.ShouldBe("System.Object[]");

                ClrObject[] refs = obj.EnumerateObjectReferences(false).ToArray();
                Assert.Single(refs);
                refs[0].Type.Name.ShouldBe("DoubleRef");
            }
        }

        [Fact]
        public void ObjectSetAddRemove()
        {
            using (DataTarget dataTarget = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;

                ObjectSet hash = new ObjectSet(heap);
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    hash.Contains(obj).ShouldBeFalse();
                    hash.Add(obj);
                    hash.Contains(obj).ShouldBeTrue();
                }

                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    hash.Contains(obj).ShouldBeTrue();
                    hash.Remove(obj);
                    hash.Contains(obj).ShouldBeFalse();
                }
            }
        }

        [Fact]
        public void ObjectSetTryAdd()
        {
            using (DataTarget dataTarget = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;

                ObjectSet hash = new ObjectSet(heap);
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    hash.Contains(obj).ShouldBeFalse();
                    hash.Add(obj).ShouldBeTrue();
                    hash.Contains(obj).ShouldBeTrue();
                    hash.Add(obj).ShouldBeFalse();
                    hash.Contains(obj).ShouldBeTrue();
                }
            }
        }

        [Fact]
        public void BuildCacheCancel()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;
                heap.StackwalkPolicy = ClrRootStackwalkPolicy.SkipStack;

                GCRoot gcroot = new GCRoot(heap);
                ulong target = gcroot.Heap.GetObjectsOfType("TargetType").Single();

                CancellationTokenSource source = new CancellationTokenSource();
                source.Cancel();

                try
                {
                    gcroot.BuildCache(source.Token);
                    throw new Exception("Should have been cancelled!");
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        [Fact]
        public void EnumerateGCRootsCancel()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;
                heap.StackwalkPolicy = ClrRootStackwalkPolicy.SkipStack;
                GCRoot gcroot = new GCRoot(runtime.Heap);

                ulong target = gcroot.Heap.GetObjectsOfType("TargetType").Single();

                CancellationTokenSource source = new CancellationTokenSource();
                source.Cancel();

                try
                {
                    gcroot.EnumerateGCRoots(target, false, source.Token).ToArray();
                    throw new Exception("Should have been cancelled!");
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        [Fact]
        public void FindSinglePathCancel()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;
                heap.StackwalkPolicy = ClrRootStackwalkPolicy.SkipStack;
                GCRoot gcroot = new GCRoot(runtime.Heap);

                CancellationTokenSource cancelSource = new CancellationTokenSource();
                cancelSource.Cancel();

                GetKnownSourceAndTarget(runtime.Heap, out ulong source, out ulong target);
                try
                {
                    gcroot.FindSinglePath(source, target, cancelSource.Token);
                    throw new Exception("Should have been cancelled!");
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        [Fact]
        public void EnumerateAllPathshCancel()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;
                heap.StackwalkPolicy = ClrRootStackwalkPolicy.SkipStack;
                GCRoot gcroot = new GCRoot(runtime.Heap);

                CancellationTokenSource cancelSource = new CancellationTokenSource();
                cancelSource.Cancel();

                GetKnownSourceAndTarget(runtime.Heap, out ulong source, out ulong target);
                try
                {
                    gcroot.EnumerateAllPaths(source, target, false, cancelSource.Token).ToArray();
                    throw new Exception("Should have been cancelled!");
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        [Fact]
        public void GCStaticRoots()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;
                heap.StackwalkPolicy = ClrRootStackwalkPolicy.SkipStack;
                GCRoot gcroot = new GCRoot(runtime.Heap);

                gcroot.ClearCache();
                gcroot.IsFullyCached.ShouldBeFalse();
                GCStaticRootsImpl(gcroot);

                gcroot.BuildCache(CancellationToken.None);

                gcroot.AllowParallelSearch = false;
                gcroot.IsFullyCached.ShouldBeTrue();
                GCStaticRootsImpl(gcroot);

                gcroot.AllowParallelSearch = true;
                gcroot.IsFullyCached.ShouldBeTrue();
                GCStaticRootsImpl(gcroot);
            }
        }

        private void GCStaticRootsImpl(GCRoot gcroot)
        {
            ulong target = gcroot.Heap.GetObjectsOfType("TargetType").Single();
            GCRootPath[] paths = gcroot.EnumerateGCRoots(target, false, CancellationToken.None).ToArray();
            Assert.Single(paths);
            GCRootPath rootPath = paths[0];

            AssertPathIsCorrect(gcroot.Heap, rootPath.Path.ToArray(), rootPath.Path.First().Address, target);
        }

        [Fact]
        public void GCRoots()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                GCRoot gcroot = new GCRoot(runtime.Heap);

                gcroot.ClearCache();
                gcroot.IsFullyCached.ShouldBeFalse();
                GCRootsImpl(gcroot);

                gcroot.BuildCache(CancellationToken.None);

                gcroot.AllowParallelSearch = false;
                gcroot.IsFullyCached.ShouldBeTrue();
                GCRootsImpl(gcroot);

                gcroot.AllowParallelSearch = true;
                gcroot.IsFullyCached.ShouldBeTrue();
                GCRootsImpl(gcroot);
            }
        }

        private void GCRootsImpl(GCRoot gcroot)
        {
            ClrHeap heap = gcroot.Heap;
            ulong target = heap.GetObjectsOfType("TargetType").Single();
            GCRootPath[] rootPaths = gcroot.EnumerateGCRoots(target, false, CancellationToken.None).ToArray();

            
#if !NETCOREAPP2_1
            rootPaths.Length.ShouldBeGreaterThanOrEqualTo(2);
#else
            rootPaths.ShouldNotBeEmpty();
#endif

            foreach (GCRootPath rootPath in rootPaths)
                AssertPathIsCorrect(heap, rootPath.Path.ToArray(), rootPath.Path.First().Address, target);

#if !NETCOREAPP2_1
            bool hasThread = false;
#endif
            bool hasStatic = false;
            foreach (GCRootPath rootPath in rootPaths)
            {
                var gcRootKind = rootPath.Root.Kind;
                switch (gcRootKind)
                {
                    case GCRootKind.Pinning:
                        hasStatic = true;
                        break;
#if !NETCOREAPP2_1
                    case GCRootKind.LocalVar:
                        hasThread = true;
                        break;
#endif
                    default:
                        throw new NotImplementedException($"Unexpected GCRootKind {gcRootKind}");
                }
            }

#if !NETCOREAPP2_1
            hasThread.ShouldBeTrue();
#endif
            hasStatic.ShouldBeTrue();
        }

        [Fact]
        public void FindPath()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                GCRoot gcroot = new GCRoot(runtime.Heap);

                gcroot.ClearCache();
                gcroot.IsFullyCached.ShouldBeFalse();
                FindPathImpl(gcroot);

                gcroot.BuildCache(CancellationToken.None);
                gcroot.IsFullyCached.ShouldBeTrue();;
                FindPathImpl(gcroot);
            }
        }

        private void FindPathImpl(GCRoot gcroot)
        {
            ClrHeap heap = gcroot.Heap;
            GetKnownSourceAndTarget(heap, out ulong source, out ulong target);

            LinkedList<ClrObject> path = gcroot.FindSinglePath(source, target, CancellationToken.None);

            AssertPathIsCorrect(heap, path.ToArray(), source, target);
        }

        [Fact]
        public void FindAllPaths()
        {
            using (DataTarget dataTarget = TestTargets.GCRoot.LoadFullDump())
            {
                ClrRuntime runtime = dataTarget.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                
                GCRoot gcroot = new GCRoot(runtime.Heap);

                gcroot.ClearCache();
                gcroot.IsFullyCached.ShouldBeFalse();
                FindAllPathsImpl(gcroot);

                gcroot.BuildCache(CancellationToken.None);
                gcroot.IsFullyCached.ShouldBeTrue();
                FindAllPathsImpl(gcroot);
            }
        }

        private void FindAllPathsImpl(GCRoot gcroot)
        {
            ClrHeap heap = gcroot.Heap;
            GetKnownSourceAndTarget(heap, out ulong source, out ulong target);

            LinkedList<ClrObject>[] paths = gcroot.EnumerateAllPaths(source, target, false, CancellationToken.None)
                .Take(4).ToArray();

            // There are exactly three paths to the object in the test target
            paths.Length.ShouldBe(3);

            foreach (LinkedList<ClrObject> path in paths)
                AssertPathIsCorrect(heap, path.ToArray(), source, target);
        }

        private static void GetKnownSourceAndTarget(ClrHeap heap, out ulong source, out ulong target)
        {
            ClrModule module = heap.Runtime.GetMainModule();
            ClrType mainType = module.GetTypeByName("GCRootTarget");

            source = mainType.GetStaticObjectValue("TheRoot").Address;
            target = heap.GetObjectsOfType("TargetType").Single();
        }

        private void AssertPathIsCorrect(ClrHeap heap, ClrObject[] path, ulong source, ulong target)
        {
            path.ShouldNotBeNull();
            (path.Length > 0).ShouldBeTrue();

            ClrObject first = path.First();
            first.Address.ShouldBe(source);

            for (int i = 0; i < path.Length - 1; i++)
            {
                ClrObject curr = path[i];
                heap.GetObjectType(curr.Address).ShouldBe(curr.Type);

                List<ulong> refs = new List<ulong>();
                curr.Type.EnumerateRefsOfObject(curr.Address, (obj, offs) => refs.Add(obj));

                ulong next = path[i + 1].Address;
                Assert.Contains(next, refs);
            }

            ClrObject last = path.Last();
            heap.GetObjectType(last.Address).ShouldBe(last.Type);
            last.Address.ShouldBe(target);
        }
    }
}