using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.IO
{
    public sealed class RiffReader
    {
        private Stream stream;
        internal BinaryReader reader;
        private List<RiffChunk> chunks = new List<RiffChunk>();
        internal SubstreamManager manager;

        public RiffReader(Stream s)
        {
            stream = s;
            reader = new BinaryReader(stream);

            // skip header
            stream.Seek(12, Origin.Current);

            manager = new SubstreamManager(stream);

            while (!stream.IsEOF())
            {
                var ch = new RiffChunkReader(this);
                chunks.Add(ch);
                stream.Seek(ch.GetStream().GetLength(), Origin.Current);
            }
        }

        public Stream BaseStream
        {
            get
            {
                return stream;
            }
        }

        public RiffChunk FindChunk(string name)
        {
            foreach (var ch in chunks)
            {
                if (ch.Name == name)
                    return ch;
            }
            throw new KeyNotFoundException("RIFF chunk '" + name + "' was not found.");
        }

        public void Close()
        {
            stream.Close();
        }
    }

    public abstract class RiffChunk
    {
        public string Name { get; protected set; }

        public abstract IO.Stream GetStream();
    }

    internal class RiffChunkReader: RiffChunk
    {
        private Stream stream;

        private static ASCIIEncoding enc = new ASCIIEncoding();

        internal RiffChunkReader(RiffReader reader)
        {
            var s = reader.BaseStream;
            var rd = reader.reader;
            Name = enc.GetString(s.Read(4));
            var ln = rd.ReadInt32();
            stream = reader.manager.GetSubstream(s.GetPosition(), ln); 
        }

        public override Stream GetStream()
        {
            return stream;
        }
    }
}
