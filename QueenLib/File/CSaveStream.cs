using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.File
{
    public class CSaveStream: IO.Stream
    {
        private IO.Stream baseStream;

        public CSaveStream(IO.Stream s)
        {
            baseStream = s;
        }

        public override void Write(byte[] bytes)
        {
            baseStream.Write(bytes);
        }

        public override void Seek(long offset, IO.Origin origin)
        {
            baseStream.Seek(offset, origin);
        }

        public override long GetPosition()
        {
            return baseStream.GetPosition();
        }

        private static UTF8Encoding enc = new UTF8Encoding();
        public void WriteString(string str)
        {
            Write(enc.GetBytes(str));
        }

        public void WriteChar(char c)
        {
            WriteString(c.ToString());
        }

        public void WriteFloat(float v)
        {
            WriteString(v.ToString());
        }

        public void WriteInt(long v)
        {
            WriteString(v.ToString());
        }
    }
}
