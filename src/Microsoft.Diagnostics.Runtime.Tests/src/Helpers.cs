﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    public static class Helpers
    {
        public static IEnumerable<ulong> GetObjectsOfType(this ClrHeap heap, string name)
        {
            return from obj in heap.EnumerateObjectAddresses()
                   let type = heap.GetObjectType(obj)
                   where type?.Name == name
                   select obj;
        }

        public static ClrObject GetStaticObjectValue(this ClrType mainType, string fieldName)
        {
            ClrStaticField field = mainType.GetStaticFieldByName(fieldName);
            ulong obj = (ulong)field.GetValue(field.Type.Heap.Runtime.AppDomains.Single());
            return new ClrObject(obj, mainType.Heap.GetObjectType(obj));
        }

        public static ClrModule GetMainModule(this ClrRuntime runtime)
        {
            return runtime.Modules.Single(m => m.FileName.EndsWith(".exe"));
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
            HashSet<T> set = new HashSet<T>();
            foreach (T t in self)
                set.Add(t);

            return set;
        }

        [CanBeNull]
        public static ClrAppDomain GetDomainByName(this ClrRuntime runtime, string domainName)
        {
            return runtime.AppDomains.Where(ad => ad.Name == domainName).SingleOrDefault();
        }

        [CanBeNull]
        public static ClrModule GetModule(this ClrRuntime runtime, string filename)
        {
            return (from module in runtime.Modules
                    let file = Path.GetFileName(module.FileName)
                    where file.Equals(filename, StringComparison.OrdinalIgnoreCase)
                    select module).SingleOrDefault();
        }

        [CanBeNull]
        public static ClrThread GetMainThread(this ClrRuntime runtime)
        {
            ClrThread thread = runtime.Threads.Where(t => !t.IsFinalizer).SingleOrDefault();
            return thread;
        }

        [CanBeNull]
        public static ClrStackFrame GetFrame(this ClrThread thread, string functionName)
        {
            return thread.StackTrace.SingleOrDefault(sf => sf.Method != null && sf.Method.Name == functionName);
        }

        public static string TestWorkingDirectory
        {
            get => _userSetWorkingPath ?? _workingPath.Value;
            set
            {
                Debug.Assert(!_workingPath.IsValueCreated);
                _userSetWorkingPath = value;
            }
        }

        private static string _userSetWorkingPath;
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

        internal static readonly string TempRoot = "clrmd_removeme_";
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