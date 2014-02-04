using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.File
{
    [Kuin.Global]
    public static class FileGlobal
    {
        private static string TranslatePath(string file)
        {
            file = file.Replace('/', System.IO.Path.DirectorySeparatorChar);
            file = file.Replace('\\', System.IO.Path.DirectorySeparatorChar);
            return file;
        }
        public static IO.Stream OpenFileForReading(string file)
        {
            file = TranslatePath(file);
            return (new IO.CliStream(System.IO.File.OpenRead(file)));
        }
        public static IO.Stream OpenFileForWriting(string file)
        {
            file = TranslatePath(file);
            return (new IO.CliStream(System.IO.File.Open(file, System.IO.FileMode.Create)));
        }
        public static byte[] ReadFile(string file)
        {
            var str = OpenFileForReading(file);
            try
            {
                var lst = new List<byte>();
                byte[] buf = null;
                while ((buf = str.Read(16384)).Length > 0)
                {
                    lst.AddRange(buf);
                }
                return lst.ToArray();
            }
            finally
            {
                str.Close();
            }
        }
        public static void WriteFile(string file, byte[] data)
        {
            var str = OpenFileForWriting(file);
            try
            {
                str.Write(data);
            }
            finally
            {
                str.Close();
            }
        }
    }
}
