using System;
using System.IO;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)]
    public static void Main(string[] args)
    {
        Foo foo = new Foo();
        
        try
        {
            Outer();    /* seq */
        }
        catch
        {
            if (new object() != new object())
                throw;
        }
        
        
        GC.KeepAlive(foo);
    }

    [MethodImpl(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)]
    private static void Outer()
    {
        Middle();    /* seq */
    }

    [MethodImpl(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)]
    private static void Middle()
    {
        Inner();    /* seq */
    }

    [MethodImpl(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)]
    private static void Inner()
    {
        try
        {
            throw new FileNotFoundException("FNF Message");    /* seq */
        }
        catch (FileNotFoundException e)
        {
            throw new InvalidOperationException("IOE Message", e);    /* seq */
        }
    }
}
