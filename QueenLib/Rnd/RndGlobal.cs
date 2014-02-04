using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Rnd
{
    public class CRnd
    {
        private Random rnd;

        public CRnd()
        {
            rnd = new Random();
        }
        public CRnd(long seed)
        {
            rnd = new Random((int)(seed ^ (seed >> 32)));
        }

        // FIXME: faster algorithm
        public static int GetBitWidth(ulong val)
        {
            int cnt = 0;
            while (val != 0)
            {
                cnt++;
                val >>= 1;
            }
            return cnt;
        }

        public long Get(long minValue, long maxValue)
        {
            if (minValue > maxValue) return Get(maxValue, minValue);
            if (maxValue == long.MaxValue && minValue == long.MinValue)
            {
                // diff becomes 2^64 and doesn't work well; corner case
                ulong v = (ulong)(uint)rnd.Next();
                v ^= (ulong)(uint)rnd.Next() << 31;
                v ^= (ulong)(uint)rnd.Next() << 62;
                return (long)v;
            }
            ulong diff = (ulong)(maxValue - minValue + 1);
            int bits = GetBitWidth(diff);
            ulong mask = (bits == 64) ? ulong.MaxValue : ((1UL << bits) - 1);

            if (bits <= 29)
            {
                uint ret;
                uint diff32 = (uint)diff;
                uint mask32 = (uint)mask;
                do
                {
                    ret = (uint)rnd.Next();
                    ret &= mask32;
                } while (ret >= diff32);
                return (long)ret + minValue;
            }
            else
            {
                ulong ret;
                do
                {
                    ret = (ulong)(uint)rnd.Next();
                    ret ^= (ulong)(uint)rnd.Next() << 31;
                    ret ^= (ulong)(uint)rnd.Next() << 62;
                } while (ret >= diff);
                return (long)ret + minValue;
            }
        }

        public float GetF()
        {
            return (float)rnd.NextDouble();
        }

    }

    [Queen.Kuin.Global]
    public static class RndGlobal
    {
        private static CRnd rnd = new CRnd();

        public static long Get(long min, long max)
        {
            return rnd.Get(min, max);
        }

        public static float GetF()
        {
            return rnd.GetF();
        }
    }
}
