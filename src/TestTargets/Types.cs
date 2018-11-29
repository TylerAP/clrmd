﻿using System;

class Types
{
    static object s_one = new object();
    static object s_two = new object();
    static object s_three = new object();

    static object[] s_array = new object[] { s_one, s_two, s_three };

    static Foo s_foo = new Foo();

    static object s_i = 42;

    public static void Main(string[] args)
    {
        GC.TryStartNoGCRegion(81908, true);
        Foo f = new Foo();
        Foo[] foos = new Foo[] { f };

        try
        {
            Inner();
        }
        catch
        {
            if (new object() != new object())
                throw;
        }

        GC.KeepAlive(foos);
    }

    private static void Inner()
    {
        throw new Exception();
    }
}
