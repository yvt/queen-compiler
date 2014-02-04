using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueenConsoleShell
{
    namespace Dbg
    {
        [Queen.Kuin.Global]
        public static class Dbg_Global
        {
            public static void Log(string s)
            {
                Console.WriteLine(s);
            }
        }
    }
}
