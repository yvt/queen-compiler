using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.File
{
    public class CLoadStream: IO.Stream
    {
        private IO.Stream baseStream;

        public CLoadStream(IO.Stream strm)
        {
            baseStream = strm;
        }

        public override byte[] Read(long size)
        {
            return baseStream.Read(size);
        }
        public override void Seek(long offset, IO.Origin origin)
        {
            baseStream.Seek(offset, origin);
        }
        public override long GetPosition()
        {
            return baseStream.GetPosition();
        }
        public override bool IsEOF()
        {
            return baseStream.IsEOF();
        }


        private static char[] defaultSplitChars = { ',' };
        private char[] splitChars = defaultSplitChars;
        public void SetSplitChars(char[] chrs)
        {
            splitChars = chrs;
        }

        public char ReadChar()
        {
            // TODO: support UTF-8
            byte[] b = Read(1);
            if (b.Length == 0)
                return '\0';
            else
                return (char)b[0];
        }

        public string ReadString()
        {
            var sb = new StringBuilder();
            char c;
            char[] chrs = splitChars;
            while (true)
            {
                c = ReadChar();
                if (c == '\0')
                    return sb.ToString();
                for (int i = 0; i < chrs.Length; i++)
                {
                    if (chrs[c] == c)
                    {
                        return sb.ToString();
                    }
                }
                sb.Append(c);
            }
        }

        public float ReadFloat()
        {
            return (float)Kuin.CompilerServices.RuntimeHelper.ParseDouble(ReadString());
        }

        public long ReadInt()
        {
            return Kuin.CompilerServices.RuntimeHelper.ParseLong(ReadString());
        }
    }
}
