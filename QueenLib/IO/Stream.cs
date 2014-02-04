using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.IO
{
    public enum Origin
    {
        Head,
        Tail,
        Current
    }
    public abstract class Stream: Kuin.CClass
    {
        public virtual byte[] Read(long size)
        {
            throw new NotImplementedException();
        }
        public virtual void Seek(long offset, Origin origin)
        {
            throw new NotImplementedException();
        }
        public virtual long GetPosition()
        {
            throw new NotImplementedException();
        }
        public virtual long GetLength()
        {
            throw new NotImplementedException();
        }
        public virtual bool IsEOF()
        {
            return GetPosition() >= GetLength();
        }

        public virtual byte[] ReadAll()
        {
            var mem = new System.IO.MemoryStream();
            byte[] buf = null;
            do
            {
                buf = Read(16384);
                if (buf.Length > 0)
                    mem.Write(buf, 0, buf.Length);
            } while (buf.Length > 0);
            return mem.ToArray();
        }


        public virtual void Write(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public virtual void Close()
        {
        }
    }
}
