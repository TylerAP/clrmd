// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    internal class StackTraceEntry
    {
        public ClrStackFrameType Kind { get; set; }
        public string ModuleString { get; set; }
        public string MethodName { get; set; }
    }

    public class MinidumpTests
    {
        static MinidumpTests() =>Helpers.InitHelpers();

        [Fact]
        public void MinidumpCallstackTest()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadMiniDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();

                ClrThread thread = runtime.GetMainThread();
                
                thread.ShouldNotBeNull();

                string[] expectedStackFrameMethodNames = IntPtr.Size == 8
                    //? new[] {"Inner", "Inner", "Middle", "Outer", "Main"}
                    ? new[] {null, "Main", null, "Inner", null, "Inner", "Middle", "Outer", "Main", null, null}
                    // TODO: line below needs to be checked
                    : new[] {"Inner", "Middle", "Outer", "Main"};

                int i = 0;

                var stackFrameMethodNames = thread.StackTrace.Select(f => f.Method?.Name).Take(50);

                stackFrameMethodNames.ShouldBe(expectedStackFrameMethodNames);

                foreach (ClrStackFrame frame in thread.StackTrace)
                {
                    if (frame.Kind == ClrStackFrameType.Runtime)
                    {
                        frame.InstructionPointer.ShouldBe(0ul);
                        frame.StackPointer.ShouldNotBe(0ul);
                    }
                    else
                    {
                        frame.InstructionPointer.ShouldNotBe(0ul);
                        frame.StackPointer.ShouldNotBe(0ul);
                        frame.Method.ShouldNotBeNull();
                        frame.Method.Type.ShouldNotBeNull();
                        frame.Method.Type.Module.ShouldNotBeNull();
#if !NETCOREAPP2_1
                        frame.Method.Name.ShouldBe(frames[i]);
#else
#endif
                        ++i;
                    }
                }
            }
        }

        [Fact]
        public void MinidumpExceptionPropertiesTest()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadMiniDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ExceptionTests.TestProperties(runtime);
            }
        }
    }
}