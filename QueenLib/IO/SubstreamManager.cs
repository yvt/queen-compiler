using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.IO
{
    public sealed class SubstreamManager
    {
        public Stream BaseStream { get; private set; }
        private Substream CurrentStream;

        public SubstreamManager(Stream str)
        {
            BaseStream = str;
        }

        internal Stream Activate(Substream s, bool alwaysSeek)
        {
            if (s != CurrentStream || alwaysSeek)
            {
                BaseStream.Seek(s.pos + s.offset, Origin.Head);
                CurrentStream = s;
            }
            return BaseStream;
        }

        public Stream GetSubstream(long start, long length)
        {
            return new Substream(this, start, length);
        }
    }

    internal sealed class Substream : Stream
    {
        internal SubstreamManager manager;
        internal long pos;
        internal readonly long offset, length;

        public Substream(SubstreamManager manager, long start, long length)
        {
            this.manager = manager;
            this.offset = start;
            this.length = length;
            pos = 0;
        }

        public override long GetPosition()
        {
            return pos;
        }

        public override long GetLength()
        {
            return length;
        }

        public override bool IsEOF()
        {
            return pos >= length;
        }

        public override byte[] Read(long size)
        {
            size = Math.Min(size, Math.Max(length - pos, 0));
            var ret = manager.Activate(this, false).Read(size);
            pos += size;
            return ret;
        }
        public override byte[] ReadAll()
        {
            return Read(Math.Max(length - pos, 0));
        }
        public override void Seek(long o, Origin origin)
        {
            switch (origin)
            {
                case Origin.Current:
                    pos += o;
                    manager.Activate(this, true);
                    break;
                case Origin.Head:
                    pos = o;
                    manager.Activate(this, true);
                    break;
                case Origin.Tail:
                    pos = length + o;
                    manager.Activate(this, true);
                    break;
            }
        }
        
    }
}
