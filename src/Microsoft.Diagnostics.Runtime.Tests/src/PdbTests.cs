﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Runtime.Utilities;
using Microsoft.Diagnostics.Runtime.Utilities.Pdb;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public class PdbTests
    {
        static PdbTests() =>
            Helpers.InitHelpers();

        [Fact]
        public void PdbEqualityTest()
        {
            // Ensure all methods in our source file is in the pdb.
            using (DataTarget dt = TestTargets.NestedException.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();

                PdbInfo[] allPdbs = runtime.Modules.Where(m => m.Pdb != null).Select(m => m.Pdb).ToArray();
                Assert.True(allPdbs.Length > 1);

                for (int i = 0; i < allPdbs.Length; i++)
                {
                    Assert.True(allPdbs[i].Equals(allPdbs[i]));
                    for (int j = i + 1; j < allPdbs.Length; j++)
                    {
                        allPdbs[i].Equals(allPdbs[j]).ShouldBeFalse();
                        allPdbs[j].Equals(allPdbs[i]).ShouldBeFalse();
                    }
                }
            }
        }

        [Fact]
        public void PdbGuidAgeTest()
        {
            PdbReader.GetPdbProperties(TestTargets.NestedException.Pdb, out Guid pdbSignature, out int pdbAge);

            // Ensure we get the same answer a different way.
            using (PdbReader pdbReader = new PdbReader(TestTargets.NestedException.Pdb))
            {
                pdbReader.Age.ShouldBe(pdbAge);
                pdbReader.Signature.ShouldBe(pdbSignature);
            }

            // Ensure the PEFile has the same signature/age.
            using (PEFile peFile = new PEFile(TestTargets.NestedException.Executable))
            {
                pdbSignature.ShouldBe(peFile.PdbInfo.Guid);
                pdbAge.ShouldBe(peFile.PdbInfo.Revision);
            }
        }

        [Fact]
        public void PdbSourceLineTest()
        {
            using (DataTarget dt = TestTargets.NestedException.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                ClrThread thread = runtime.GetMainThread();

                HashSet<int> sourceLines = new HashSet<int>();
                using (PdbReader reader = new PdbReader(TestTargets.NestedException.Pdb))
                {
                    reader.Sources.Single().Name.ShouldBe(TestTargets.NestedException.Source, StringCompareShould.IgnoreCase);

                    IEnumerable<PdbFunction> functions = from frame in thread.StackTrace
                                                         where frame.Kind != ClrStackFrameType.Runtime
                                                         select reader.GetFunctionFromToken(frame.Method.MetadataToken);

                    foreach (PdbFunction function in functions)
                    {
                        PdbSequencePointCollection sourceFile = function.SequencePoints.Single();

                        foreach (int line in sourceFile.Lines.Select(l => l.LineBegin))
                            sourceLines.Add(line);
                    }
                }

                int curr = 0;
                foreach (string line in File.ReadLines(TestTargets.NestedException.Source))
                {
                    curr++;
                    if (line.Contains("/* seq */"))
                        Assert.Contains(curr, sourceLines);
                }
            }
        }

        [Fact]
        public void PdbMethodTest()
        {
            // Ensure all methods in our source file is in the pdb.
            using (DataTarget dt = TestTargets.NestedException.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.SingleOrDefault()?.CreateRuntime();
                runtime.ShouldNotBeNull();
                ClrModule module = runtime.Modules.Single(m => m.Name.Equals(TestTargets.NestedException.Executable, StringComparison.OrdinalIgnoreCase));
                ClrType type = module.GetTypeByName("Program");

                using (PdbReader pdb = new PdbReader(TestTargets.NestedException.Pdb))
                {
                    foreach (ClrMethod method in type.Methods)
                    {
                        // ignore inherited methods and constructors
                        if (method.Type != type || method.IsConstructor || method.IsClassConstructor)
                            continue;

                        pdb.GetFunctionFromToken(method.MetadataToken).ShouldNotBeNull();
                    }
                }
            }
        }
    }
}