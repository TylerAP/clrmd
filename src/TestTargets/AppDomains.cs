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

#if !NETCOREAPP2_1
        AppDomain domain = AppDomain.CreateDomain("Second AppDomain");
#else
        AppDomain domain = AppDomain.CurrentDomain;
#endif

        string asmPath = Path.Combine(Environment.CurrentDirectory, "bin", "x" + (IntPtr.Size==8 ? "64" : "86"), "NestedException.dll");
        
        domain.ExecuteAssembly(asmPath);

        while (true)
            Thread.Sleep(250);
    }
}