using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Time
{
    [Kuin.Global]
    public static class TimeGlobal
    {
        public static long Sys()
        {
            // FIXME: according to the specification, Sys should return win32's GetTickCount().
            return (DateTime.Now.Ticks - new DateTime(1970, 1, 1).Ticks) / 10000;
        }

        public static long Now()
        {
            return (long)((DateTime.Now.ToOADate() - new DateTime(1970, 1, 1).ToOADate()) * 86400.0);
        }
    }
}
