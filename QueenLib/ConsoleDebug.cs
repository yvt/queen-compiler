using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.ConsoleDebug
{
    [Kuin.Global]
    public static class DbgGlobal
    {
        public static string ReadLine()
        {
            return System.Console.ReadLine();
        }
        public static void Write(string s)
        {
            System.Console.Write(s);
        }
        public static void WriteLine(string s)
        {
            System.Console.WriteLine(s);
        }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Log(string s)
        {
            WriteLine(s);
        }
        
    }
}
