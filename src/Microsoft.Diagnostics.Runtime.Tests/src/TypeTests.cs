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
    public class TypeTests
    {
        static TypeTests() =>
            Helpers.InitHelpers();

        [Fact]
        public void IntegerObjectClrType()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrHeap heap = runtime.Heap;
                Assert.NotNull(heap);

                ClrModule typesExeModule = runtime.GetModule("types.dll");
                Assert.NotNull(typesExeModule);

                ClrStaticField field = typesExeModule.GetTypeByName("Types").GetStaticFieldByName("s_i");

                ClrAppDomain appDomain = runtime.AppDomains.SingleOrDefault();
                Assert.NotNull(appDomain);

                ulong addr = (ulong)field.GetValue(appDomain);
                ClrType type = heap.GetObjectType(addr);
                Assert.NotNull(type);
                Assert.True(type.IsPrimitive);
                Assert.False(type.IsObjectReference);
                Assert.False(type.IsValueClass);

                object value = type.GetValue(addr);
                value.ToString().ShouldBe("42");
                Assert.IsType<int>(value);
                ((int)value).ShouldBe(42);

                Assert.Contains(addr, heap.EnumerateObjectAddresses());
            }
        }

        [Fact]
        public void ArrayComponentTypeTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                // Ensure that we always have a component for every array type.
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    ClrType type = heap.GetObjectType(obj);
                    Assert.True(!type.IsArray || type.ComponentType != null);

                    foreach (ClrInstanceField field in type.Fields)
                    {
                        Assert.NotNull(field.Type);
                        Assert.True(!field.Type.IsArray || field.Type.ComponentType != null);
                        Assert.Same(heap, field.Type.Heap);
                    }
                }

                foreach (ClrModule module in runtime.Modules)
                {
                    foreach (ClrType type in module.EnumerateTypes())
                    {
                        Assert.True(!type.IsArray || type.ComponentType != null);
                        Assert.Same(heap, type.Heap);
                    }
                }
            }
        }

        [Fact]
        public void ComponentType()
        {
            // Simply test that we can enumerate the heap.

            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    ClrType type = heap.GetObjectType(obj);
                    Assert.NotNull(type);

                    if (type.IsArray || type.IsPointer)
                        Assert.NotNull(type.ComponentType);
                    else
                        Assert.Null(type.ComponentType);
                }
            }
        }

        [Fact]
        public void TypeEqualityTest()
        {
            // This test ensures that only one ClrType is created when we have a type loaded into two different AppDomains with two different
            // method tables.

            const string TypeName = "Foo";
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrHeap heap = runtime.Heap;

                ClrType[] types = (from obj in heap.EnumerateObjectAddresses()
                                   let t = heap.GetObjectType(obj)
                                   where t.Name == TypeName
                                   select t).ToArray();

                types.Length.ShouldBe(2);
#if !NETCOREAPP2_1
                Assert.NotSame(types[0], types[1]);
#endif

                ClrModule module = runtime.Modules.SingleOrDefault(m => Path.GetFileName(m.FileName).Equals("sharedlibrary.dll", StringComparison.OrdinalIgnoreCase));
                ClrType typeFromModule = module.GetTypeByName(TypeName);

                typeFromModule.Name.ShouldBe(TypeName);
                typeFromModule.ShouldBe(types[0]);
            }
        }

        [Fact]
        public void VariableRootTest()
        {
            // Test to make sure that a specific static and local variable exist.

            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrHeap heap = runtime.Heap;
                heap.StackwalkPolicy = ClrRootStackwalkPolicy.Exact;

                IEnumerable<ClrRoot> fooRoots = from root in heap.EnumerateRoots()
                                                where root.Type.Name == "Foo"
                                                select root;

                ClrRoot staticRoot = fooRoots.SingleOrDefault(r => r.Kind == GCRootKind.StaticVar);
                Assert.NotNull(staticRoot);
                Assert.Contains("s_foo", staticRoot.Name);

                ClrRoot[] arr = fooRoots.Where(r => r.Kind == GCRootKind.LocalVar).ToArray();

                ClrRoot[] localVarRoots = fooRoots.Where(r => r.Kind == GCRootKind.LocalVar).ToArray();

                ClrThread thread = runtime.GetMainThread();
                Assert.NotNull(thread);

                // NOTE: this is using a helper, it was previously using .Single, now .FirstOrDefault
                ClrStackFrame main = thread.GetFrame("Main");
                ClrStackFrame inner = thread.GetFrame("Inner");

                ulong low = thread.StackBase;
                ulong high = thread.StackLimit;

                // Account for different platform stack direction.
                if (low > high)
                {
                    ulong tmp = low;
                    low = high;
                    high = tmp;
                }

                foreach (ClrRoot localVarRoot in localVarRoots)
                    Assert.True(low <= localVarRoot.Address && localVarRoot.Address <= high);
            }
        }

        [Fact]
        public void EETypeTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                HashSet<ulong> methodTables = (from obj in heap.EnumerateObjectAddresses()
                                               let type = heap.GetObjectType(obj)
                                               where !type.IsFree
                                               select heap.GetMethodTable(obj)).Unique();

                Assert.DoesNotContain(0ul, methodTables);

                foreach (ulong mt in methodTables)
                {
                    ClrType type = heap.GetTypeByMethodTable(mt);
                    ulong eeclass = heap.GetEEClassByMethodTable(mt);
                    eeclass.ShouldNotBe(0ul);

                    heap.GetMethodTableByEEClass(eeclass).ShouldNotBe(0ul);
                }
            }
        }

        [Fact]
        public void MethodTableHeapEnumeration()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                foreach (ClrType type in heap.EnumerateObjectAddresses().Select(obj => heap.GetObjectType(obj)).Unique())
                {
                    type.MethodTable.ShouldNotBe(0ul);

                    ClrType typeFromHeap;

                    if (type.IsArray)
                    {
                        ClrType componentType = type.ComponentType;
                        Assert.NotNull(componentType);

                        typeFromHeap = heap.GetTypeByMethodTable(type.MethodTable, componentType.MethodTable);
                    }
                    else
                    {
                        typeFromHeap = heap.GetTypeByMethodTable(type.MethodTable);
                    }

                    typeFromHeap.MethodTable.ShouldBe(type.MethodTable);
                    Assert.Same(type, typeFromHeap);
                }
            }
        }

        [Fact]
        public void GetObjectMethodTableTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                int i = 0;
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    i++;
                    ClrType type = heap.GetObjectType(obj);

                    if (type.IsArray)
                    {
                        bool result = heap.TryGetMethodTable(obj, out ulong mt, out ulong cmt);

                        Assert.True(result);
                        mt.ShouldNotBe(0ul);
                        mt.ShouldBe(type.MethodTable);

                        Assert.Same(type, heap.GetTypeByMethodTable(mt, cmt));
                    }
                    else
                    {
                        ulong mt = heap.GetMethodTable(obj);

                        mt.ShouldNotBe(0ul);
                        Assert.Contains(mt, type.EnumerateMethodTables());

                        Assert.Same(type, heap.GetTypeByMethodTable(mt));
                        Assert.Same(type, heap.GetTypeByMethodTable(mt, 0));

                        bool res = heap.TryGetMethodTable(obj, out ulong mt2, out ulong cmt);

                        Assert.True(res);
                        mt2.ShouldBe(mt);
                        cmt.ShouldBe(0ul);
                    }
                }
            }
        }

        [Fact]
        public void EnumerateMethodTableTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrHeap heap = runtime.Heap;

                var objs = heap.EnumerateObjectAddresses().Select(addr => heap.GetObjectType(addr)).ToArray();

                ulong[] fooObjects = heap.EnumerateObjectAddresses()
                    .Where(obj => heap.GetObjectType(obj)?.Name == "Foo")
                    .Select(obj => obj)
                    .ToArray();

                fooObjects.ShouldNotBeEmpty();

                ClrType fooType = heap.GetObjectType(fooObjects[0]);

                // There are exactly two Foo objects in the process, one in each app domain (but not under .net core).
                // They will have different method tables.
                fooObjects.Length.ShouldBe(2);

#if !NETCOREAPP2_1
                Assert.NotSame(fooType, heap.GetObjectType(fooObjects[1]));
#endif

                ClrRoot appDomainsFoo = heap
                    .EnumerateRoots(true)
                    .SingleOrDefault(root => root.Kind == GCRootKind.StaticVar && root.Type == fooType);
                Assert.NotNull(appDomainsFoo);

                ulong nestedExceptionFoo = fooObjects.SingleOrDefault(obj => obj != appDomainsFoo.Object);
                ClrType nestedExceptionFooType = heap.GetObjectType(nestedExceptionFoo);

#if !NETCOREAPP2_1
                Assert.NotSame(nestedExceptionFooType, appDomainsFoo.Type);
#endif

                ulong nestedExceptionFooMethodTable = dt.DataReader.ReadPointerUnsafe(nestedExceptionFoo);
                ulong appDomainsFooMethodTable = dt.DataReader.ReadPointerUnsafe(appDomainsFoo.Object);

#if !NETCOREAPP2_1
                // These are in different domains and should have different type handles:
                ( appDomainsFooMethodTable).ShouldNotBe(nestedExceptionFooMethodTable);
#endif

                // The MethodTable returned by ClrType should always be the method table that lives in the "first"
                // AppDomain (in order of ClrAppDomain.Id).
                fooType.MethodTable.ShouldBe(appDomainsFooMethodTable);

                // Ensure that we enumerate two type handles and that they match the method tables we have above.
                ulong[] methodTableEnumeration = fooType.EnumerateMethodTables().ToArray();
#if !NETCOREAPP2_1
                ( methodTableEnumeration.Length).ShouldBe(2);
#else
                Assert.Single(methodTableEnumeration);
#endif

#if !NETCOREAPP2_1
                // These also need to be enumerated in ClrAppDomain.Id order
                ( methodTableEnumeration[0]).ShouldBe(appDomainsFooMethodTable);
                ( methodTableEnumeration[1]).ShouldBe(nestedExceptionFooMethodTable);
#else
                methodTableEnumeration[0].ShouldBe(appDomainsFooMethodTable);
                nestedExceptionFooMethodTable.ShouldBe(appDomainsFooMethodTable);
#endif
            }
        }

        [Fact]
        public void FieldNameAndValueTests()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrHeap heap = runtime.Heap;

                ClrAppDomain domain = runtime.AppDomains.SingleOrDefault();
                Assert.NotNull(domain);

                ClrModule sharedLibraryModule = runtime.GetModule("sharedlibrary.dll");
                Assert.NotNull(sharedLibraryModule);
                ClrType fooType = sharedLibraryModule.GetTypeByName("Foo");

                ClrModule typesExeModule = runtime.GetModule("types.dll");
                Assert.NotNull(typesExeModule);
                ulong obj = (ulong)typesExeModule.GetTypeByName("Types").GetStaticFieldByName("s_foo").GetValue(runtime.AppDomains.Single());

                Assert.Same(fooType, heap.GetObjectType(obj));

                TestFieldNameAndValue(fooType, obj, "i", 42);
                TestFieldNameAndValue(fooType, obj, "s", "string");
                TestFieldNameAndValue(fooType, obj, "b", true);
                TestFieldNameAndValue(fooType, obj, "f", 4.2f);
                TestFieldNameAndValue(fooType, obj, "d", 8.4);
            }
        }

        public ClrInstanceField TestFieldNameAndValue<T>(ClrType type, ulong obj, string name, T value)
        {
            ClrInstanceField field = type.GetFieldByName(name);
            Assert.NotNull(field);
            field.Name.ShouldBe(name);

            object v = field.GetValue(obj);
            Assert.NotNull(v);
            Assert.IsType<T>(v);

            v.ShouldBe(value);

            return field;
        }
    }

    public class ArrayTests
    {
        [Fact]
        public void ArrayOffsetsTest()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;

                ClrAppDomain domain = runtime.AppDomains.Single();

                ClrModule typesModule = runtime.GetModule("types.dll");
                ClrType type = heap.GetTypeByName("Types");

                ulong s_array = (ulong)type.GetStaticFieldByName("s_array").GetValue(domain);
                ulong s_one = (ulong)type.GetStaticFieldByName("s_one").GetValue(domain);
                ulong s_two = (ulong)type.GetStaticFieldByName("s_two").GetValue(domain);
                ulong s_three = (ulong)type.GetStaticFieldByName("s_three").GetValue(domain);

                ulong[] expected = {s_one, s_two, s_three};

                ClrType arrayType = heap.GetObjectType(s_array);
                Assert.NotNull(arrayType);

                for (int i = 0; i < expected.Length; i++)
                {
                    ((ulong)arrayType.GetArrayElementValue(s_array, i)).ShouldBe(expected[i]);

                    ulong address = arrayType.GetArrayElementAddress(s_array, i);
                    ulong value = dt.DataReader.ReadPointerUnsafe(address);

                    address.ShouldNotBe(0ul);
                    value.ShouldBe(expected[i]);
                }
            }
        }

        [Fact]
        public void ArrayLengthTest()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;

                ClrAppDomain domain = runtime.AppDomains.Single();

                ClrModule typesModule = runtime.GetModule("types.dll");
                ClrType type = heap.GetTypeByName("Types");

                ulong s_array = (ulong)type.GetStaticFieldByName("s_array").GetValue(domain);
                ClrType arrayType = heap.GetObjectType(s_array);

                Assert.NotNull(arrayType);
                arrayType.GetArrayLength(s_array).ShouldBe(3);
            }
        }

        [Fact]
        public void ArrayReferenceEnumeration()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrHeap heap = runtime.Heap;

                ClrAppDomain domain = runtime.AppDomains.Single();

                ClrModule typesModule = runtime.GetModule("types.dll");
                ClrType type = heap.GetTypeByName("Types");

                ulong s_array = (ulong)type.GetStaticFieldByName("s_array").GetValue(domain);
                ulong s_one = (ulong)type.GetStaticFieldByName("s_one").GetValue(domain);
                ulong s_two = (ulong)type.GetStaticFieldByName("s_two").GetValue(domain);
                ulong s_three = (ulong)type.GetStaticFieldByName("s_three").GetValue(domain);

                ClrType arrayType = heap.GetObjectType(s_array);
                Assert.NotNull(arrayType);

                List<ulong> objs = new List<ulong>();
                arrayType.EnumerateRefsOfObject(s_array, (obj, offs) => objs.Add(obj));

                // We do not guarantee the order in which these are enumerated.
                objs.Count.ShouldBe(3);
                Assert.Contains(s_one, objs);
                Assert.Contains(s_two, objs);
                Assert.Contains(s_three, objs);
            }
        }
    }
}