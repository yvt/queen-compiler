using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language
{
    delegate void TokenizerErrorEventHandler(object sender, TokenizerErrorEventArgs args);

    class TokenizerErrorEventArgs : EventArgs
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }

        public TokenizerErrorEventArgs()
        {
        }
        public TokenizerErrorEventArgs(int line, int column, string message)
        {
            Line = line;
            Column = column;
            Message = message;
        }
    }

}

