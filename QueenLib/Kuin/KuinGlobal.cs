using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    [Global]
    public static class KuinGlobal
    {
        

        private static string hashChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        private static void DoHashPart(char[] arr, int startIdx, byte[] bytes, int startBytes)
        {
            ulong bits = bytes[0];
            bits |= ((uint)bytes[1]) << 8;
            bits |= ((uint)bytes[2]) << 16;
            bits |= ((uint)bytes[3]) << 24;
            bits |= ((uint)bytes[4]) << 32;
            arr[startIdx] = hashChars[(int)(bits & 0x1f)];
            bits >>= 5; startIdx += 1;
            arr[startIdx] = hashChars[(int)(bits & 0x1f)];
            bits >>= 5; startIdx += 1;
            arr[startIdx] = hashChars[(int)(bits & 0x1f)];
            bits >>= 5; startIdx += 1;
            arr[startIdx] = hashChars[(int)(bits & 0x1f)];
            bits >>= 5; startIdx += 1;
            arr[startIdx] = hashChars[(int)(bits & 0x1f)];
            bits >>= 5; startIdx += 1;
            arr[startIdx] = hashChars[(int)(bits & 0x1f)];
            bits >>= 5; startIdx += 1;
            arr[startIdx] = hashChars[(int)(bits & 0x1f)];
            bits >>= 5; startIdx += 1;
            arr[startIdx] = hashChars[(int)(bits & 0x1f)];
        }

        public static char[] Hash(byte[] bytes)
        {
            // FIXME: use correct algorithm?
            var sha1 = new System.Security.Cryptography.SHA1Managed();
            byte[] dat = sha1.ComputeHash(bytes);
            char[] chars = new char[32];
            DoHashPart(chars, 0, dat, 0);
            DoHashPart(chars, 8, dat, 5);
            DoHashPart(chars, 16, dat, 10);
            DoHashPart(chars, 24, dat, 15);
            return chars;
        }

    }
}
