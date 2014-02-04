using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin.CompilerServices
{
    public static class RuntimeHelper
    {
        public static int IntPower32(int a, int b)
        {
            if (b == 0)
                return 1;
            else if (b < 0)
                return 0;
            else if (b < 4)
            {
                int vl = a;
                while (b > 1)
                {
                    vl *= a; b--;
                }
                return vl;
            }
            else
            {
                int k = b >> 1;
                return IntPower32(a, k) * IntPower32(a, b - k);
            }
        }

        public static uint IntPowerU32(uint a, uint b)
        {
            if (b == 0)
                return 1;
            else if (b < 0)
                return 0;
            else if (b < 4)
            {
                uint vl = a;
                while (b > 1)
                {
                    vl *= a; b--;
                }
                return vl;
            }
            else
            {
                uint k = b >> 1;
                return IntPowerU32(a, k) * IntPowerU32(a, b - k);
            }
        }

        public static long IntPower64(long a, long b)
        {
            if (b == 0)
                return 1;
            else if (b < 0)
                return 0;
            else if (b < 4)
            {
                long vl = a;
                while (b > 1)
                {
                    vl *= a; b--;
                }
                return vl;
            }
            else
            {
                long k = b >> 1;
                return IntPower64(a, k) * IntPower64(a, b - k);
            }
        }

        public static long IntPower64Checked(long a, long b)
        {
            checked
            {
                if (b == 0)
                    return 1;
                else if (b < 0)
                    return 0;
                else if (b < 4)
                {
                    long vl = a;
                    while (b > 1)
                    {
                        vl *= a; b--;
                    }
                    return vl;
                }
                else
                {
                    long k = b >> 1;
                    return IntPower64Checked(a, k) * IntPower64Checked(a, b - k);
                }
            }
        }

        public static ulong IntPowerU64(ulong a, ulong b)
        {
            if (b == 0)
                return 1;
            else if (b < 0)
                return 0;
            else if (b < 4)
            {
                ulong vl = a;
                while (b > 1)
                {
                    vl *= a; b--;
                }
                return vl;
            }
            else
            {
                ulong k = b >> 1;
                return IntPowerU64(a, k) * IntPowerU64(a, b - k);
            }
        }

        public static T[] ArraySub<T>(T[] arr, long _start, long _length)
        {
            int start = checked((int)_start);
            int end = checked((int)(_start + _length));
            if (start < 0) start = 0;
            if (end > arr.Length) end = arr.Length;
            if (end <= start)
                return new T[] { };
            T[] ret = new T[end - start];
            Array.Copy(arr, start, ret, 0, end - start);
            return ret;
        }

        public static T[] ArrayConcat<T>(T[] arr1, T[] arr2)
        {
            var arr = new T[arr1.Length + arr2.Length];
            Array.Copy(arr1, arr, arr1.Length);
            Array.Copy(arr2, 0, arr, arr1.Length, arr2.Length);
            return arr;
        }

        public static int ArrayCompare<A,B>(A[] arr1, B[] arr2) where A:IComparable<B>
        {
            for (int i = 0; i < arr1.Length && i < arr2.Length; i++)
            {
                A v1 = arr1[i];
                B v2 = arr2[i];
                int c = v1.CompareTo(v2);
                if (c > 0)
                    return c;
                else if (c < 0)
                    return c;
            }

            if (arr1.Length > arr2.Length)
                return 1;
            else if (arr1.Length < arr2.Length)
                return -1;
            return 0;
        }

        private static Random rand = new Random();
        public static void ArrayShuffle<A>(A[] arr)
        {
            var rd = rand;
            for (int i = 1; i < arr.Length; i++)
            {
                int idx = rd.Next(i + 1);
                if (idx < i)
                {
                    A tmp = arr[idx];
                    arr[idx] = arr[i];
                    arr[i] = tmp;
                }
            }
        }

        public static string StringSub(string str, long start, long length)
        {
            checked
            {
                return str.Substring((int)start, (int)length);
            }
        }

        private enum PositiveNumberMode
        {
            ForceSign,
            Whitespace,
            Default
        }

        public enum FillMode
        {
            WhitespaceRightAligned,
            Zero,
            WhitespaceLeftAligned
        }

        public enum ExponentalMode
        {
            Auto,
            Never,
            Always
        }

        private struct NumericFormat
        {
            public PositiveNumberMode PositiveNumberMode;
            public FillMode FillMode;
            public int IntegralDigits;
            public int FractionDigits;
            public ExponentalMode ExponentalMode;
            public char ExponentalChar;
            public int Radix;
            public bool UpperCaseDigits;

            public NumericFormat(string str)
            {
                PositiveNumberMode = PositiveNumberMode.Default;
                FillMode = FillMode.WhitespaceRightAligned;
                IntegralDigits = 0;
                FractionDigits = 6;
                ExponentalMode = ExponentalMode.Never;
                ExponentalChar = 'E';
                UpperCaseDigits = false;

                Radix = 10;
                if (str.Length == 0)
                    return;
                int index = 0;
                switch (str[index])
                {
                    case '+':
                        PositiveNumberMode = RuntimeHelper.PositiveNumberMode.ForceSign;
                        index++;
                        break;
                    case ' ':
                        PositiveNumberMode = RuntimeHelper.PositiveNumberMode.Whitespace;
                        index++;
                        break;
                }

                if (index >= str.Length) return;

                switch (str[index])
                {
                    case '-':
                        FillMode = RuntimeHelper.FillMode.WhitespaceLeftAligned;
                        index++;
                        break;
                    case '0':
                        FillMode = RuntimeHelper.FillMode.Zero;
                        index++;
                        break;
                }

                if (index >= str.Length) return;

                int digits = 0;
                while (index < str.Length)
                {
                    char c = str[index];
                    if (c >= '0' && c <= '9')
                    {
                        digits = checked((digits * 10) + (c - '0'));
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
                IntegralDigits = digits;

                if (index >= str.Length) return;

                if (str[index] == '.')
                {
                    // fractions
                    digits = 0;
                    index++;
                    while (index < str.Length)
                    {
                        char c = str[index];
                        if (c >= '0' && c <= '9')
                        {
                            digits = checked((digits * 10) + (c - '0'));
                            index++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    FractionDigits = digits;

                    if (index >= str.Length) return;
                }

                switch (str[index])
                {
                    case 'e':
                        ExponentalMode = RuntimeHelper.ExponentalMode.Always;
                        ExponentalChar = 'e';
                        index++;
                        break;
                    case 'E':
                        ExponentalMode = RuntimeHelper.ExponentalMode.Always;
                        ExponentalChar = 'E';
                        index++;
                        break;
                    case 'a':
                        ExponentalMode = RuntimeHelper.ExponentalMode.Always;
                        ExponentalChar = 'e';
                        Radix = 16;
                        index++;
                        break;
                    case 'A':
                        ExponentalMode = RuntimeHelper.ExponentalMode.Always;
                        ExponentalChar = 'E';
                        Radix = 16;
                        index++;
                        break;
                    case 'g':
                        ExponentalMode = RuntimeHelper.ExponentalMode.Auto;
                        ExponentalChar = 'e';
                        index++;
                        break;
                    case 'G':
                        ExponentalMode = RuntimeHelper.ExponentalMode.Auto;
                        ExponentalChar = 'E';
                        index++;
                        break;
                    case 'f':
                        UpperCaseDigits = false;
                        index++;
                        break;
                    case 'F':
                        UpperCaseDigits = true;
                        index++;
                        break;
                    case 'd':
                        UpperCaseDigits = false;
                        index++;
                        break;
                    case 'D':
                        UpperCaseDigits = true;
                        index++;
                        break;
                    case 'x':
                        UpperCaseDigits = false;
                        Radix = 16;
                        index++;
                        break;
                    case 'X':
                        UpperCaseDigits = true;
                        Radix = 16;
                        index++;
                        break;
                }

                // read radix
                digits = 0;
                while (index < str.Length)
                {
                    char c = str[index];
                    if (c >= '0' && c <= '9')
                    {
                        digits = checked((digits * 10) + (c - '0'));
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
                if(digits > 1 && digits <= 36)
                    Radix = digits;
            }
        }

        public static string ToStrF(ulong value, string format)
        {
            var fmt = new NumericFormat(format);
            var digits = new List<char>();
            ulong radix = (ulong)fmt.Radix;
            string digitChars = fmt.UpperCaseDigits ?
                "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ":
                "0123456789abcdefghijklmnopqrstuvwxyz";

            while (value > 0)
            {
                ulong digit = value % radix;
                value -= digit;
                value /= radix;
                digits.Add(digitChars[(int)digit]);
            }

            var sb = new StringBuilder(digits.Count * 2); 
            
            if (fmt.FillMode == FillMode.WhitespaceRightAligned)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append(' ');
            }

            switch (fmt.PositiveNumberMode)
            {
                case PositiveNumberMode.ForceSign:
                    sb.Append('+');
                    break;
                case PositiveNumberMode.Whitespace:
                    sb.Append(' ');
                    break;
            }

            if (fmt.FillMode == FillMode.Zero)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append('0');
            }
            for (int i = digits.Count - 1; i >= 0; i--)
                sb.Append(digits[i]);

            if (fmt.FillMode == FillMode.WhitespaceLeftAligned)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append(' ');
            }
            return sb.ToString();
        }

        public static string ToStrF(long ivalue, string format)
        {
            var fmt = new NumericFormat(format);
            var digits = new List<char>();
            ulong radix = (ulong)fmt.Radix;
            string digitChars = fmt.UpperCaseDigits ?
                "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ" :
                "0123456789abcdefghijklmnopqrstuvwxyz";
            bool negative = ivalue < 0;
            ulong value;
            if (ivalue == long.MinValue)
            {
                value = (ulong)long.MaxValue + 1;
            }
            else if (negative)
            {
                value = (ulong)(-ivalue);
            }
            else
            {
                value = (ulong)ivalue;
            }

            if (value == 0)
            {
                digits.Add('0');
            }
            while (value > 0)
            {
                ulong digit = value % radix;
                value -= digit;
                value /= radix;
                digits.Add(digitChars[(int)digit]);
            }

            var sb = new StringBuilder(digits.Count * 2);

            if (fmt.FillMode == FillMode.WhitespaceRightAligned)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append(' ');
            }

            if (negative)
            {
                sb.Append('-');
            }
            else
            {
                switch (fmt.PositiveNumberMode)
                {
                    case PositiveNumberMode.ForceSign:
                        sb.Append('+');
                        break;
                    case PositiveNumberMode.Whitespace:
                        sb.Append(' ');
                        break;
                }
            }

            if (fmt.FillMode == FillMode.Zero)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append('0');
            }
            for (int i = digits.Count - 1; i >= 0; i--)
                sb.Append(digits[i]);

            if (fmt.FillMode == FillMode.WhitespaceLeftAligned)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append(' ');
            }
            return sb.ToString();
        }



        private static string ToStrF(double ivalue, NumericFormat fmt)
        {
            if (double.IsPositiveInfinity(ivalue))
                return "+Infinity";
            else if (double.IsNegativeInfinity(ivalue))
                return "-Infinity";
            else if (double.IsNaN(ivalue))
                return "NaN";

            var digits = new List<char>();
            double radix = (double)fmt.Radix;
            string digitChars = fmt.UpperCaseDigits ?
                "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ" :
                "0123456789abcdefghijklmnopqrstuvwxyz";
            bool negative = ivalue < 0.0;
            double value;
            if (negative)
            {
                value = (double)(-ivalue);
            }
            else
            {
                value = (double)ivalue;
            }

            int exponent = 0;
            if (value != 0.0 && fmt.ExponentalMode == ExponentalMode.Always)
            {
                while (value < 1.0)
                {
                    value *= radix; exponent--;
                }
                while (value >= radix)
                {
                    value /= radix; exponent++;
                }
            }

            double integralPart = Math.Floor(value);
            double fractPart = value - integralPart;
            if (integralPart < 1.0)
            {
                digits.Add('0');
            }
            else
            {
                while (integralPart >= 1.0)
                {
                    double v2 = Math.Floor(integralPart / radix);
                    int digit = (int)(integralPart - v2 * radix);
                    integralPart = v2;
                    digits.Add(digitChars[(int)digit]);
                }
            }

            var sb = new StringBuilder(digits.Count * 2);

            if (fmt.FillMode == FillMode.WhitespaceRightAligned)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append(' ');
            }

            if (negative)
            {
                sb.Append('-');
            }
            else
            {
                switch (fmt.PositiveNumberMode)
                {
                    case PositiveNumberMode.ForceSign:
                        sb.Append('+');
                        break;
                    case PositiveNumberMode.Whitespace:
                        sb.Append(' ');
                        break;
                }
            }

            if (fmt.FillMode == FillMode.Zero)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append('0');
            }
            for (int i = digits.Count - 1; i >= 0; i--)
                sb.Append(digits[i]);

            // fraction part
            if (fmt.FractionDigits > 0)
                sb.Append('.');
            for (int i = fmt.FractionDigits; i > 0; i--)
            {
                fractPart *= radix;
                double v = Math.Floor(fractPart);
                int digit = (int)v;
                sb.Append(digitChars[(int)digit]);
                fractPart -= v;
            }

            if (fmt.ExponentalMode == ExponentalMode.Always && exponent != 0)
            {
                sb.Append(fmt.ExponentalChar);
                if (exponent > 0)
                {
                    sb.Append('+');
                    sb.Append(exponent.ToString());
                }
                else
                {
                    sb.Append(exponent.ToString());
                }
            }

            if (fmt.FillMode == FillMode.WhitespaceLeftAligned)
            {
                for (int i = fmt.IntegralDigits - digits.Count; i > 0; i--)
                    sb.Append(' ');
            }
            return sb.ToString();
        }

        public static string ToStrF(double value, string format)
        {
            var fmt = new NumericFormat(format);
            if (fmt.ExponentalMode == ExponentalMode.Auto)
            {
                fmt.ExponentalMode = ExponentalMode.Always;
                string s1 = ToStrF(value, fmt);
                fmt.ExponentalMode = ExponentalMode.Never;
                string s2 = ToStrF(value, fmt);
                return (s1.Length < s2.Length) ? s1 : s2;
            }
            else
            {
                return ToStrF(value, fmt);
            }
        }

        public static string ToStrF(int value, string format)
        {
            return ToStrF((long)value, format);
        }

        public static string ToStrF(short value, string format)
        {
            return ToStrF((long)value, format);
        }

        public static string ToStrF(sbyte value, string format)
        {
            return ToStrF((long)value, format);
        }

        public static string ToStrF(uint value, string format)
        {
            return ToStrF((long)value, format);
        }

        public static string ToStrF(ushort value, string format)
        {
            return ToStrF((long)value, format);
        }

        public static string ToStrF(byte value, string format)
        {
            return ToStrF((long)value, format);
        }

        public static string ToStrF(float value, string format)
        {
            return ToStrF((double)value, format);
        }

        public static long ParseLong(string str)
        {
            int pos = 0;
            bool negative = false;
            if (str.Length == 0)
            {
                return 0;
            }
            if (str[0] == '-')
            {
                pos++;
                negative = true;
            }

            long val = 0;
            checked
            {
                while (pos < str.Length)
                {
                    char c = str[pos];
                    if (c >= '0' && c <= '9')
                    {
                        val *= 10;
                        int v = c - '0';
                        if (negative) val -= v; else val += v;
                        pos++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (pos >= str.Length || str[pos] != '#' || val < -36 || val > 36 || (val > -2 && val < 2))
            {
                return val;
            }
            int radix = (int)Math.Abs(val);
            val = 0;
            pos++;
            checked
            {
                while (pos < str.Length)
                {
                    char c = str[pos];
                    int v;
                    if (c >= '0' && c <= '9')
                        v = c - '0';
                    else if (c >= 'a' && c <= 'z')
                        v = c - 'a' + 10;
                    else if (c >= 'A' && c <= 'Z')
                        v = c - 'A' + 10;
                    else
                        return val;
                        
                    if (v >= radix)
                    {
                        return val;
                    }
                    val *= 10;
                    if (negative) val -= v; else val += v;
                    pos++;
                }
            }
            return val;
        }

        public static double ParseDouble(string str)
        {
            // TODO: support radix notation
            return double.Parse(str);
        }

        public static void AssertionFailed()
        {
            throw new CAssertFailExcpt();
        }

        public static Delegate VirtualMemberFunctionPointerError(object ob)
        {
            throw new InvalidOperationException("Attempt to obtain the function pointer for a virtual member function is illegal.");
        }
    }
}
