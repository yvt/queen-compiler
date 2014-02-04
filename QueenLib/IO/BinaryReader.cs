using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.IO
{
    public sealed class BinaryReader
    {
        Stream baseStream;
        byte[] buffer;

        public BinaryReader(Stream str)
        {
            baseStream = str;
        }

        private byte[] FillBuffer(int bytes)
        {
            buffer = baseStream.Read(bytes);
            return buffer;
        }

        public byte[] ReadBytes(long bytes)
        {
            return baseStream.Read(bytes);
        }

        public byte ReadUInt8()
        {
            var b = FillBuffer(1);
            return b[0];
        }

        public ushort ReadUInt16()
        {
            var b = FillBuffer(2);
            uint v = b[0];
            v |= ((uint)b[1]) << 8;
            return (ushort)v;
        }

        public uint ReadUInt32()
        {
            var b = FillBuffer(4);
            uint v = b[0];
            v |= ((uint)b[1]) << 8;
            v |= ((uint)b[2]) << 16;
            v |= ((uint)b[3]) << 24;
            return v;
        }

        public ulong ReadUInt64()
        {
            var b = FillBuffer(8);
            ulong v = b[0];
            v |= ((ulong)b[1]) << 8;
            v |= ((ulong)b[2]) << 16;
            v |= ((ulong)b[3]) << 24;
            v |= ((ulong)b[4]) << 32;
            v |= ((ulong)b[5]) << 40;
            v |= ((ulong)b[6]) << 48;
            v |= ((ulong)b[7]) << 56;
            return v;
        }

        public sbyte ReadInt8()
        {
            return (sbyte)ReadUInt8();
        }

        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        public long ReadInt64()
        {
            return (long)ReadUInt64();
        }

        public long ReadInt()
        {
            long val = 0;
            uint b;
            bool negative = false;
            b = ReadUInt8();
            if ((b & 0x40) != 0)
            {
                // negative value
                val = (long)((int)(b | 0xffffff80));
                negative = true;
            }
            else
            {
                val = (long)(b & 0x3f);
            }
            if ((b & 0x80) == 0)
            {
                return val;
            }

            int shift = 6;
            while (true)
            {
                b = ReadUInt8();
                if (negative)
                {
                    val -= ((long)(b & 0x7f)) << shift;
                }
                else
                {
                    val += ((long)(b & 0x7f)) << shift;
                }
                if ((b & 0x80) == 0)
                {
                    break;
                }
                else
                {
                    shift += 7;
                }
            }

            return val;
        }

        public unsafe float ReadFloat()
        {
            uint v = ReadUInt32();
            return *(float*)&v;
        }

        public unsafe double ReadDouble()
        {
            ulong v = ReadUInt64();
            return *(double*)&v;
        }
    }
}
