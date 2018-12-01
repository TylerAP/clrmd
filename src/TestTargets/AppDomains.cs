using System;
using System.IO;
using System.Reflection;
using System.Threading;

class Program
{
    static Foo s_foo = new Foo();
    
    static Program() {} // beforefieldinit
    
    static void Main(string[] args)
    {
        string codebase = Assembly.GetExecutingAssembly().CodeBase;

        if (codebase.StartsWith("file://"))
            codebase = codebase.Substring(8).Replace('/', '\\');

#if !NETCOREAPP2_1
        AppDomain domain = AppDomain.CreateDomain("Second AppDomain");
#else
        AppDomain domain = AppDomain.CurrentDomain;
#endif

        domain.ExecuteAssembly(Path.Combine(Path.GetDirectoryName(codebase), "NestedException.dll"));

        while (true)
            Thread.Sleep(250);
    }
}