using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language
{
    public delegate void ParserErrorEventHandler(object sender, ParserErrorEventArgs args);

    public class ParserErrorEventArgs : EventArgs
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }

        public ParserErrorEventArgs()
        {
        }
        public ParserErrorEventArgs(int line, int column, string message)
        {
            Line = line;
            Column = column;
            Message = message;
        }
    }
}
