// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
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
        [Fact]
        public void MinidumpCallstackTest()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadMiniDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                Assert.NotNull(runtime);

                ClrThread thread = runtime.GetMainThread();
                
                Assert.NotNull(thread);

                string[] expectedStackFrameMethodNames = IntPtr.Size == 8
                    //? new[] {"Inner", "Inner", "Middle", "Outer", "Main"}
                    ? new[] {null, "Main", null, "Inner", null, "Inner", "Middle", "Outer", "Main", null, null}
                    // TODO: line below needs to be checked
                    : new[] {"Inner", "Middle", "Outer", "Main"};

                int i = 0;

                var stackFrameMethodNames = thread.StackTrace.Select(f => f.Method?.Name);

                stackFrameMethodNames.Should().Equal(expectedStackFrameMethodNames);

                foreach (ClrStackFrame frame in thread.StackTrace)
                {
                    if (frame.Kind == ClrStackFrameType.Runtime)
                    {
                        Assert.Equal(0ul, frame.InstructionPointer);
                        Assert.NotEqual(0ul, frame.StackPointer);
                    }
                    else
                    {
                        Assert.NotEqual(0ul, frame.InstructionPointer);
                        Assert.NotEqual(0ul, frame.StackPointer);
                        Assert.NotNull(frame.Method);
                        Assert.NotNull(frame.Method.Type);
                        Assert.NotNull(frame.Method.Type.Module);
#if !NETCOREAPP2_1
                        Assert.Equal(frames[i],  frame.Method.Name);
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