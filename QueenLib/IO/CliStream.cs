using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.IO
{
    public class CliStream: Stream
    {
        private System.IO.Stream baseStream;
        public CliStream(System.IO.Stream stream)
        {
            baseStream = stream;
        }

        public override byte[] Read(long size)
        {
            var buf = new byte[(int)size];
            int cnt = baseStream.Read(buf, 0, buf.Length);
            if (cnt < 0)
                return new byte[] { };
            Array.Resize<byte>(ref buf, cnt);
            return buf;
        }
        public override void Seek(long offset, Origin origin)
        {
            switch (origin)
            {
                case Origin.Head:
                    baseStream.Seek(offset, System.IO.SeekOrigin.Begin);
                    break;
                case Origin.Tail:
                    baseStream.Seek(offset, System.IO.SeekOrigin.End);
                    break;
                case Origin.Current:
                    baseStream.Seek(offset, System.IO.SeekOrigin.Current);
                    break;
            }
        }
        public override long GetPosition()
        {
            return baseStream.Position;
        }
        public override long GetLength()
        {
            return baseStream.Length;
        }
        public override bool IsEOF()
        {
            return baseStream.Position >= baseStream.Length;
        }

        public override void Write(byte[] bytes)
        {
            baseStream.Write(bytes, 0, bytes.Length);
        }

        public override void Close()
        {
            baseStream.Close();
        }
    }
}
