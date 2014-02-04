using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Lib
{
    [Queen.Kuin.Global]
    public static class LibGlobal
    {
        public const float Pi = 3.14159265358979323846f;
        public const float E = 2.71828182845904523536f;

        public static float Cos(float v)
        {
            return (float)Math.Cos(v);
        }

        public static float Sin(float v)
        {
            return (float)Math.Sin(v);
        }

        public static float Tan(float v)
        {
            return (float)Math.Tan(v);
        }

        public static float ASin(float v)
        {
            return (float)Math.Asin(v);
        }

        public static float ACos(float v)
        {
            return (float)Math.Acos(v);
        }

        public static float ATan(float v)
        {
            return (float)Math.Atan(v);
        }

        public static float Sqrt(float v)
        {
            return (float)Math.Sqrt(v);
        }

        public static float Exp(float v)
        {
            return (float)Math.Exp(v);
        }

        public static float Ln(float v)
        {
            return (float)Math.Log(v);
        }

        public static float Log(float v, float bas)
        {
            return (float)Math.Log(v, bas);
        }

        public static float Floor(float v)
        {
            return (float)Math.Floor(v);
        }


        public static float Ceil(float v)
        {
            return (float)Math.Ceiling(v);
        }



        public static void Rot(out float x, out float y, float cx, float cy, float angle)
        {
            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);

            x = cx * c - cy * s;
            y = cx * s + cy * c;
        }

        public static float InvRot(float x, float y)
        {
            return (float)Math.Atan2(y, x);
        }

        public static float Hypot(float x, float y)
        {
            return (float)Math.Sqrt(x * x + y * y);
        }

        public static bool Chase(ref float x, float target, float velo)
        {
            if (x > target)
            {
                x -= velo;
                if (x < target)
                {
                    x = target;
                    return true;
                }
            }
            else
            {
                x += velo;
                if (x > target)
                {
                    x = target;
                    return true;
                }
            }
            return false;
        }

        private static Random rand = new Random();

        public static long[] RndOrder(long num)
        {
            if (num < 0)
            {
                throw new ArgumentOutOfRangeException("num");
            }

            int cnt = checked((int)num);
            long[] ret = new long[cnt];
            for (int i = 0; i < cnt; i++)
            {
                ret[i] = i;
            }

            var rd = rand;
            for (int i = 1; i < cnt; i++)
            {
                int idx = rd.Next(i + 1);
                if (idx < i)
                {
                    long tmp = ret[idx];
                    ret[idx] = ret[i];
                    ret[i] = tmp;
                }
            }

            return ret;
        }


    }
}
