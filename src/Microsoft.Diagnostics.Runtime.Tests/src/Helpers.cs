﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Shouldly;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public static class Helpers
    {
        static Helpers()
        {
            AppDomain.CurrentDomain.UnhandledException += (appDomObj, unhandled) =>
                {
                    Action x = () => throw (Exception)unhandled.ExceptionObject;
                    x.ShouldNotThrow();
                };
        }

        public static void InitHelpers()
        {
            // invoke static constructor once
        }

        public static IEnumerable<ulong> GetObjectsOfType(this ClrHeap heap, string name)
        {
            return heap.EnumerateObjectAddresses()
                .Where(obj => heap.GetObjectType(obj)?.Name == name);
        }

        public static ClrObject GetStaticObjectValue(this ClrType mainType, string fieldName)
        {
            ClrStaticField field = mainType.GetStaticFieldByName(fieldName);
            ClrAppDomain appDom = field.Type.Heap.Runtime.AppDomains.Single();
            ulong obj = (ulong)field.GetValue(appDom);
            return new ClrObject(obj, mainType.Heap.GetObjectType(obj));
        }

        public static ClrModule GetMainModule(this ClrRuntime runtime)
        {
            // TODO: how do you for-sure identify a main module? does this even make sense?
            return runtime.Modules.Skip(1).First();

            //return runtime.Modules.Single(m => m.FileName.EndsWith(".exe"));
        }

        public static ClrMethod GetMethod(this ClrType type, string name)
        {
            return GetMethods(type, name).Single();
        }

        public static IEnumerable<ClrMethod> GetMethods(this ClrType type, string name)
        {
            return type.Methods.Where(m => m.Name == name);
        }

        public static HashSet<T> Unique<T>(this IEnumerable<T> self)
        {
            return new HashSet<T>(self);
        }

        [CanBeNull]
        public static ClrAppDomain GetDomainByName(this ClrRuntime runtime, string domainName)
        {
            return runtime.AppDomains.SingleOrDefault(ad => ad.Name == domainName);
        }

        [CanBeNull]
        public static ClrModule GetModule(this ClrRuntime runtime, string filename)
        {
            return runtime.Modules.Take(50)
                .SingleOrDefault
                (
                    module =>
                        Path.GetFileName(module.FileName)
                            .Equals(filename, StringComparison.OrdinalIgnoreCase)
                );
        }

        [CanBeNull]
        public static ClrThread GetMainThread(this ClrRuntime runtime)
        {
            ClrThread thread = runtime.Threads.SingleOrDefault(t => !t.IsFinalizer);
            return thread;
        }

        [CanBeNull]
        public static ClrStackFrame GetFrame(this ClrThread thread, string functionName)
        {
            return thread.StackTrace.FirstOrDefault(sf => sf.Method != null && sf.Method.Name == functionName);
        }

        public static string TestWorkingDirectory
        {
            get => _userSetWorkingPath ?? _workingPath.Value;
            
            /*
            set
            {
                Debug.Assert(!_workingPath.IsValueCreated);
                _userSetWorkingPath = value;
            }
            */
        }

#pragma warning disable 649
        private static string _userSetWorkingPath;
#pragma warning restore 649

        private static readonly Lazy<string> _workingPath = new Lazy<string>(CreateWorkingPath, true);

        private static string CreateWorkingPath()
        {
            Random r = new Random();
            string path;
            do
            {
                path = Path.Combine(Environment.CurrentDirectory, TempRoot + r.Next());
            } while (Directory.Exists(path));

            Directory.CreateDirectory(path);
            return path;
        }

        internal const string TempRoot = "clrmd_removeme_";
        
    }

    public class GlobalCleanup
    {
        public static void AssemblyCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            foreach (string directory in Directory.GetDirectories(Environment.CurrentDirectory))
                if (directory.Contains(Helpers.TempRoot))
                    Directory.Delete(directory, true);
        }
    }
}