using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.IO
{
    public sealed class BinaryWriter
    {
        Stream baseStream;
        byte[][] bufs = new byte[][] {
            new byte[0], new byte[1], new byte[2], new byte[3],
            new byte[4], new byte[5], new byte[6], new byte[7],
            new byte[8], new byte[9], new byte[10]
        };

        public BinaryWriter(Stream baseStream)
        {
            this.baseStream = baseStream;
        }

        public void WriteBytes(byte[] bytes)
        {
            baseStream.Write(bytes);
        }

        public void WriteUInt8(byte b)
        {
            var buf = bufs[1];
            buf[0] = b;
            baseStream.Write(buf);
        }
        public void WriteUInt16(ushort b)
        {
            var buf = bufs[2];
            buf[0] = (byte)(b);
            buf[1] = (byte)(b >> 8);
            baseStream.Write(buf);
        }
        public void WriteUInt32(uint b)
        {
            var buf = bufs[4];
            buf[0] = (byte)(b);
            buf[1] = (byte)(b >> 8);
            buf[2] = (byte)(b >> 16);
            buf[3] = (byte)(b >> 24);
            baseStream.Write(buf);
        }
        public void WriteUInt64(ulong b)
        {
            var buf = bufs[8];
            buf[0] = (byte)(b);
            buf[1] = (byte)(b >> 8);
            buf[2] = (byte)(b >> 16);
            buf[3] = (byte)(b >> 24);
            buf[4] = (byte)(b >> 32);
            buf[5] = (byte)(b >> 40);
            buf[6] = (byte)(b >> 48);
            buf[7] = (byte)(b >> 56);
            baseStream.Write(buf);
        }

        public void WriteInt8(sbyte b)
        {
            WriteUInt8((byte)b);
        }
        public void WriteInt16(short b)
        {
            WriteUInt16((ushort)b);
        }
        public void WriteInt32(int b)
        {
            WriteUInt32((uint)b);
        }
        public void WriteInt64(long b)
        {
            WriteUInt64((ulong)b);
        }

        public void WriteInt(long v)
        {
            var buf = bufs[10];
            bool negative = v < 0;
            if (negative)
            {
                buf[0] = (byte)((v & 0x3f) | 0x40);
                v &= ~(long)0x3f;
                v = -v; // this negation always succeeds
            }
            else
            {
                buf[0] = (byte)(v & 0x3f);
            }
            v >>= 6;

            int idx = 0;

            while (v != 0)
            {
                buf[idx++] |= 0x80; // continuation
                buf[idx] = (byte)(v & 0x7f);
                v >>= 7;
            }

            var buf2 = bufs[idx + 1];
            Array.Copy(buf, buf2, buf2.Length);

            WriteBytes(buf2);
        }

        public unsafe void WriteFloat(float v)
        {
            WriteUInt32(*(uint*)&v);
        }

        public unsafe void WriteDouble(double v)
        {
            WriteUInt64(*(ulong*)&v);
        }

    }
}
