// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class ExceptionTests
    {
        static ExceptionTests() =>Helpers.InitHelpers();
        
        [Fact]
        public void ExceptionPropertyTest()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                
                TestProperties(runtime);
            }
        }

        internal static void TestProperties(ClrRuntime runtime)
        {
            ClrThread thread = runtime.Threads.Where(t => !t.IsFinalizer).SingleOrDefault();
            thread.ShouldNotBeNull();
            
            ClrException ex = thread.CurrentException;
            ex.ShouldNotBeNull();

            ExceptionTestData testData = TestTargets.NestedExceptionData;
            ex.Message.ShouldBe(testData.OuterExceptionMessage);
            ex.Type.Name.ShouldBe(testData.OuterExceptionType);
            ex.Inner.ShouldNotBeNull();
        }
    }
}