using System;
using System.IO;
using System.Reflection;
using System.Threading;

class Program
{
    
    
    static void Main(string[] args)
    {
        Console.WriteLine("Hello");
        
        Console.CancelKeyPress += (con, ckp) => Environment.Exit(0);
        
        while (true)
        {
            if (Console.In.Peek() != -1)
            {
                if (Console.In.Read() == 0x03)
                {
                    Environment.Exit(0);
                    return;
                }
            }

            Thread.Sleep(15);
        }
    }
}